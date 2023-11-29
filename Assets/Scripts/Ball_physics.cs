using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Ball_physics : MonoBehaviour
{
    public MeshGenerator mesh;

    [SerializeField] private float _radius = 12; // Radius of the ball


    //
    [SerializeField] private Vector3 _currentfPosition;
    [SerializeField] private Vector3 _previousPosition;
    [SerializeField] private Vector3 _currentVelocity;
    [SerializeField] private Vector3 _previousVelocity;
    [SerializeField] private Vector3 Acceleration;
    private float frictionCoefficient;
    private float accelerationThreshold = 0.1f; // Threshold to determine if the ball has stopped
    private float stoppedDuration;
    private float timeThreshold = 1.0f; // Duration threshold to consider the ball as stopped

    #region VassDrag

    [SerializeField] private bool createTrail = true;

    [SerializeField] private float _vassDragDT = 5;
    private float _vassDragTimer = 1;

    public LineRenderer splineRenderer;
    [SerializeField] private int _steps = 5;
    [SerializeField] float splineHeight = 5f;
   // private List<Vector3> ControlPoints; // List to store control points
    private bool BallStoppedSliding;

    #endregion

    #region Storm

    [SerializeField] private bool shouldMove = true;
    
    [SerializeField]private List<Vector3> RainPositions = new List<Vector3>(); // Store the ball's positions
    [SerializeField] private int _degree = 2;
    [SerializeField]private float[] Knots;

    #endregion

    //
    [SerializeField] private int _currentIndex; //(current triangle)
    [SerializeField] private int _previousIndex; //(Previous triangle)
    [SerializeField] private Vector3 _previousNormal;
    [SerializeField] private Vector3 _currentNormal;
    private bool isFalling = true;
    private Vector3 collisionPoint; // Store collision point with the mesh

    private readonly Vector3 gravity = new Vector3(0, -9.81f, 0); // Custom gravitational acceleration


    private float _startHeight;

    private void Start()
    {
        _currentfPosition = transform.position;
        _previousPosition = _currentfPosition;

        transform.position = _currentfPosition;

        BallStoppedSliding = false;
    }


    private void FixedUpdate()
    {
        if (isFalling)
        {
            ApplyCustomGravity();

            if (IsCloseToMesh())
            {
                isFalling = false;
                _currentfPosition = transform.position;
                _previousPosition = _currentfPosition;
                //Debug.Log("Ball touched the mesh at: " + collisionPoint);

                _vassDragTimer = 0;
                if (createTrail)
                {
                    GameObject splineObject = new GameObject("SplineCurve");
                    splineObject.transform.SetParent(transform);
                    splineRenderer = splineObject.AddComponent<LineRenderer>();
                    if (splineRenderer != null)
                    {
                        splineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                        splineRenderer.startColor = Color.blue;
                        splineRenderer.endColor = Color.blue;
                        splineRenderer.startWidth = 0.3f;
                        splineRenderer.endWidth = 0.3f;
                    }
                }
            }
        }
        else if (mesh)
        {
            if (shouldMove)
            {
                Correction();
                Move();
                if (Acceleration.magnitude < accelerationThreshold)
                {
                    stoppedDuration += Time.fixedDeltaTime;

                    if (stoppedDuration >= timeThreshold)
                    {
                        BallStopped();
                    }
                }
                else
                {
                    stoppedDuration = 0f;
                }
            }


            // Stop ball movement if the flag is set to false
            if (!shouldMove)
            {
                _currentVelocity = Vector3.zero; // Stop ball velocity
                Acceleration = Vector3.zero; // Stop ball acceleration
                _previousVelocity = Vector3.zero; // Stop ball velocity
                _previousPosition = _currentfPosition; // Stop ball position
            }


            _vassDragTimer -= Time.deltaTime;

            if (_vassDragTimer <= 0 && BallStoppedSliding == false)
            {
                _vassDragTimer = _vassDragDT;
                //Debug.Log("saving rains position" + _currentfPosition);
                RainPositions.Add(_currentfPosition);

                if (RainPositions.Count > 2)
                {
                    // Assuming _degree is set properly before calling CalculateKnots()
                    CalculateKnots(); // Call CalculateKnots() after collecting enough RainPositions
                   
                    UpdateSplineRenderer(); // Update the LineRenderer positions with the B-spline points
                }
            }
        }
    }


    private void Awake()
    {
        mesh = FindObjectOfType<MeshGenerator>();
    }


    private void Correction()
    {
        Debug.Log("Entering debug log");

        // Find the point on the ground right under the center of the ball
        var p = new Vector3(_currentfPosition.x,
            GetValidSurfaceHeight(new Vector3(_currentfPosition.x, 0, _currentfPosition.z)),
            _currentfPosition.z);

        // Distance vector from center to p
        var dist = _currentfPosition - p;

        // Distance vector projected onto normal
        var b = Vector3.Dot(dist, _currentNormal) * _currentNormal;

        if (b.magnitude <= _radius)
        {
            Debug.Log("Actually correcting!");

            _currentfPosition = p + _radius * _currentNormal;
            transform.position = _currentfPosition;
        }
        else
        {
            // Additional check: If the ball is under the terrain, place it on top
            float surfaceHeight = GetValidSurfaceHeight(new Vector3(_currentfPosition.x, 0, _currentfPosition.z));
            if (_currentfPosition.y < surfaceHeight)
            {
                _currentfPosition.y = surfaceHeight + _radius; // Place the ball on top of the terrain
                _previousPosition = _currentfPosition;
                transform.position = _currentfPosition;
            }
        }
    }


    private void Move()
    {
        //Debug.Log("Ball is rolling" + _currentfPosition);
        // Iterate through each triangle 
        for (var i = 0; i < mesh.triangles.Length; i += 3)
        {
            // get vertices of the triangle


            var p0 = mesh.vertices[mesh.triangles[i]];
            var p1 = mesh.vertices[mesh.triangles[i + 1]];
            var p2 = mesh.vertices[mesh.triangles[i + 2]];

            // save the balls position in the xz-plane
            //var pos = new Vector2(_currentfPosition.x, _currentfPosition.z);
            var pos = new Vector2(_currentfPosition.x, _currentfPosition.z);

            // Find which triangle the ball is currently on with barycentric coordinates
            var baryCoords = mesh.BarycentricCoordinates(
                new Vector2(p0.x, p0.z),
                new Vector2(p1.x, p1.z),
                new Vector2(p2.x, p2.z),
                pos
            );


            if (baryCoords is { x: >= 0.0f, y: >= 0.0f, z: >= 0.0f })
            {
                //beregne normal
                _currentIndex = i / 3;
                _currentNormal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

                //bergen akselerasjonesvektor - ligning (8.12)
                // Vector3 acceleration = (1 / ballMass) * (normalVector + Physics.gravity);
                //Oppdaterer hastigheten og posisjon
                Acceleration = -Physics.gravity.y * new Vector3(_currentNormal.x * _currentNormal.y,
                    _currentNormal.y * _currentNormal.y - 1,
                    _currentNormal.z * _currentNormal.y);


                //Oppdater hastighet og posisjon
                //ligning (8.14) og (8.15)
                _currentVelocity = _previousVelocity + Acceleration * Time.fixedDeltaTime;
                Vector3 friction = -_currentVelocity.normalized * frictionCoefficient;
                _currentVelocity += friction * Time.fixedDeltaTime;
                _previousVelocity = _currentVelocity;

                _currentfPosition = _previousPosition + _previousVelocity * Time.fixedDeltaTime;
                _previousPosition = _currentfPosition;
                transform.position = _currentfPosition;

                if (_currentIndex != _previousIndex)
                {
                    //timeBallArray.Add(timeBall); Trying to add Time ball to a list for each triangle

                    //ballen har Rullet over til en ny trekant
                    //beregn normaler  til kollisjonsplanet
                    // se ligningen(8.17)

                    var n = (_currentNormal + _previousNormal).normalized;


                    //Korrigere posisjon oppover i normalens retning
                    //oppdater hastighetsverkoren (8.16)
                    var afterCollisionVelocity = _currentVelocity - 2f * Vector3.Dot(_currentVelocity, n) * n;
                    //oppdater posisjon i retning den nye hastighestvektoren
                    _currentVelocity = afterCollisionVelocity + Acceleration * Time.fixedDeltaTime;
                    _previousVelocity = _currentVelocity;

                    _currentfPosition = _previousPosition + _previousVelocity * Time.fixedDeltaTime;
                    _previousPosition = _currentfPosition;
                    transform.position = _currentfPosition;
                }

                //Oppdater gammel  normal og indeks
                _previousNormal = _currentNormal;
                _previousIndex = _currentIndex;
            }
        }
    }


    int FindKnotInterval(float x)
    {
        int my = RainPositions.Count - 1;
        while (x < Knots[my])
            my--;
        return my;
    }

    public Vector3 EvaluateBSplineSimple(float x)
    {
        int my = FindKnotInterval(x);

        List<Vector3> a = new List<Vector3>(_degree + 1);
        for (int i = 0; i <= _degree * 4; i++)
        {
            a.Add(Vector3.zero);
        }

        for (int i = 0; i <= _degree ; i++)
        {
            a[_degree - i] = RainPositions[my - i];
        }
        
        for (int k = _degree; k > 0; k--)
        {
            int j = my - k;
            for (int i = 0; i < k; i++)
            {
                j++;
                float w = (x - Knots[j]) / (Knots[j + k] - Knots[j]);
                a[i] = a[i] * (1 - w) + a[i + 1] * w;
            }

            
        }
        return a[0];
        // Return a default value in case the loops don't execute
        //return Vector3.zero;
    }

// Calculate Knots array based on RainPositions count and degree
    private void CalculateKnots()
    {
        int n = RainPositions.Count;
        int d = _degree;

        int knotsCount = n + d + 1;
        Knots = new float[knotsCount];

        for (int i = 0; i < knotsCount; i++)
        {
            if (i < d + 1)
            {
                Knots[i] = 0; // Knots from 0 to d are set to 0
            }
            else if (i > n)
            {
                Knots[i] = 1; // Knots after the last control point are set to 1
            }
            else
            {
                Knots[i] = (i - d) / (float)(n - d); // Internal knots calculation
            }
        }
    }

    private float GetValidSurfaceHeight(Vector3 point)
    {
        // Attempt to find a valid surface height
        float surfaceHeight = mesh.GetSurfaceHeight(new Vector2(point.x, point.z));
        int maxAttempts = 10; // Maximum attempts to find a valid height

        while (surfaceHeight <= 0 && maxAttempts > 0)
        {
            // Adjust the point slightly and try again
            point += new Vector3(0.1f, 0.1f, 0.1f); // Adjust this offset as needed
            surfaceHeight = mesh.GetSurfaceHeight(new Vector2(point.x, point.z));
            maxAttempts--;
        }

        // Return either the found surface height or a default value
        return surfaceHeight > 0 ? surfaceHeight + _radius : 0.0f;
    }


    private void BallStopped()
    {
        // Logic to execute when the ball is considered stopped

        BallStoppedSliding = true;
    }

    private void ApplyCustomGravity()
    {
        // Apply custom gravity to the ball
        _currentVelocity += gravity * Time.fixedDeltaTime;
        _currentfPosition += _currentVelocity * Time.fixedDeltaTime;

        transform.position = _currentfPosition;
    }

    private bool IsCloseToMesh()
    {
        // Get the surface height directly under the ball's center
        Vector2 currentPositionXZ = new Vector2(_currentfPosition.x, _currentfPosition.z);
        float surfaceHeight = mesh.GetSurfaceHeight(currentPositionXZ);


        // Check if the ball's vertical position is close enough to the surface of the generated mesh
        return _currentfPosition.y <= surfaceHeight + _radius; // Adjust the threshold value as needed
    }

 void UpdateSplineRenderer()
{
    int numberOfPoints = _steps; // Change this value as needed for the resolution of the curve
    splineRenderer.positionCount = numberOfPoints;

    for (int i = 0; i < numberOfPoints; i++)
    {
        float t = i / (float)(numberOfPoints - 1);
        
        Vector3 pointOnSpline = EvaluateBSplineSimple(t);
        pointOnSpline.y += splineHeight; // Adding splineHeight to the y-coordinate

        // Ensure the correct position is being updated
        splineRenderer.SetPosition(i, pointOnSpline);
        
    }
    
}
    
  
}