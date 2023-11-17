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


    //
    [SerializeField] private int _currentIndex; //(current triangle)
    [SerializeField] private int _previousIndex; //(Previous triangle)
    [SerializeField] private Vector3 _previousNormal;
    [SerializeField] private Vector3 _currentNormal;


    //Start locations

    [SerializeField] public Vector3 _startLocation = new(177f, 433f,404f);
    private float _startHeight;

    private void Start()
    {
        
        var _startHeight = mesh.GetSurfaceHeight(new Vector3(_startLocation.x,_startLocation.y, _startLocation.z));
        _currentfPosition = new Vector3(_startLocation.x, _startHeight + _radius, _startLocation.y);
        _previousPosition = _currentfPosition;

        transform.position = _currentfPosition;
    }

    private void Awake()
    {
    }

    private void FixedUpdate()
    {
        if (mesh)
        {
            Correction();
            Move();
        }
    }

    private void Update()
    {
        Vector3 startlocationv3 = _startLocation;
        //Debug.Log("Acceleration" + Acceleration.magnitude);
        //Debug.Log("length of currentvelocity" + _currentVelocity.magnitude);
        //Debug.Log("Length between startlocation and endlocation" + (startlocationv3 - _currentfPosition).magnitude);
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
        // Iterate through each triangle 
        for (var i = 0; i < mesh.triangles.Length; i += 3)
        {
            // get vertices of the triangle
            
            
            var p0 = mesh.vertices[mesh.triangles[i]];
            var p1 = mesh.vertices[mesh.triangles[i + 1]];
            var p2 = mesh.vertices[mesh.triangles[i + 2]];

            // save the balls position in the xz-plane
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
