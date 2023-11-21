using System;
using System.Collections.Generic;
using UnityEngine;

public class Ball_physics : MonoBehaviour
{
    public MeshGenerator mesh;

    private readonly float _radius = 6f;
    

    [SerializeField] private Vector3 hitLocation;

    //
    [SerializeField] private Vector3 _currentfPosition;
    [SerializeField] private Vector3 _previousPosition;
    [SerializeField] private Vector3 _currentVelocity;
    [SerializeField] private Vector3 _previousVelocity;
    [SerializeField] private float timeBall;
    [SerializeField] private List<float> timeBallArray;
    [SerializeField] private Vector3 Acceleration;
    [SerializeField] private float frictionCoefficient; 


    //
    [SerializeField] private int _currentIndex; //(current triangle)
    [SerializeField] private int _previousIndex; //(Previous triangle)
    [SerializeField] private Vector3 _previousNormal;
    [SerializeField] private Vector3 _currentNormal;
    private bool isFalling = true;
    private Vector3 collisionPoint; // Store collision point with the mesh
    
private readonly Vector3 gravity = new Vector3(0, -9.81f, 0); // Custom gravitational acceleration

    //Start locations

    //[SerializeField] public Vector3 _startLocation = new(177f, 433f,404f);
    private float _startHeight;

    private void Start()
    {
        
        var _startHeight = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        //_currentfPosition = new Vector3(transform.position.x, transform.position.y, transform.position.y);
        _currentfPosition = transform.position;
        //_currentfPosition = new Vector3(transform.position.x, _startHeight + _radius, transform.position.y);
        _previousPosition = _currentfPosition;

        transform.position = _currentfPosition;

        
    }

     private void FixedUpdate()
     { 
         
        if (isFalling)
        {
            ApplyCustomGravity(); // Apply gravity until the ball touches the mesh

            // Check if the ball is close enough to the mesh to start collision checks
            if (IsCloseToMesh())
            {
                isFalling = false; // Set the flag to false to indicate the ball touched the mesh
                //collisionPoint = _currentfPosition; // Store the collision point
                _currentfPosition = transform.position; // Store the collision point
                _previousPosition = _currentfPosition;
                Debug.Log("Ball touched the mesh at: " + collisionPoint);
                
            }
        }
        else if (mesh) // Once the ball touches the mesh, perform movement and collision checks
        {
           // _currentfPosition = collisionPoint; // Set the ball's position to the collision point 
           // _previousPosition = _currentfPosition;
            Debug.Log("Ball is now rolling at: " + _currentfPosition);
            Correction(); // Check for collisions and adjust position if intersecting
            Move();       // Move the ball based on the calculated physics
        }
    }

    private void Awake()
    {
        mesh = FindObjectOfType<MeshGenerator>();
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
private void Correction()
{
    // Get the current ball position in 2D (x, z)
    Vector2 currentPositionXZ = new Vector2(_currentfPosition.x, _currentfPosition.z);

    // Find the surface height directly under the ball's center
    float surfaceHeight = mesh.GetSurfaceHeight(currentPositionXZ);

    // Calculate the contact point on the mesh
    Vector3 contactPoint = new Vector3(_currentfPosition.x, surfaceHeight, _currentfPosition.z);

    // Calculate the distance vector from the ball's center to the contact point
    Vector3 distanceVector = contactPoint - _currentfPosition;

    // Project the distance vector onto the surface normal
    float projectionMagnitude = Vector3.Dot(distanceVector, _currentNormal);
    Vector3 projectedVector = projectionMagnitude * _currentNormal;

    // If the ball is intersecting with the mesh
    if (projectedVector.magnitude <= _radius)
    {
        // Move the ball up along the surface normal by the amount needed to prevent intersection
        Vector3 correction = _radius * _currentNormal;
        _currentfPosition += correction;

        // Update the ball's position
        transform.position = _currentfPosition;
    }
}


 
    private void Move()
    {
        Debug.Log("Ball is rolling" + _currentfPosition);
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
                timeBall += Time.fixedDeltaTime;

                hitLocation = baryCoords;
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
}
