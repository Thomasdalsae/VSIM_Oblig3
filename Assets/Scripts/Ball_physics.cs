using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Ball_physics : MonoBehaviour
{
    public MeshGenerator mesh;

    [SerializeField] private float _radius = 12; // Radius of the ball


    #region Movement

    [SerializeField] private Vector3 _currentfPosition;
    [SerializeField] private Vector3 _previousPosition;
    [SerializeField] private Vector3 _currentVelocity;
    [SerializeField] private Vector3 _previousVelocity;
    [SerializeField] private Vector3 Acceleration;
    private float frictionCoefficient;
    private float accelerationThreshold = 0.1f; // Grense på ajelerasjonen for å bestemme om ballen har stoppet
    private float stoppedDuration;
    private float timeThreshold = 1.0f; // hvor lenge ballen må ligge stille for å bli stoppet

    #endregion

    #region VassDrag

    [SerializeField]
    private bool
        createTrail = true; //Om man skal ha trail på ballen eller ikke (siden jeg bruker samme script for alle ballene)

    [SerializeField] private float _vassDragDT = 5; // Hvor ofte vi skal lagre posisjonen til ballen
    private float _vassDragTimer = 1; // Teller ned til vi skal lagre posisjonen til ballen

    public LineRenderer splineRenderer;
    [SerializeField] private int _steps = 5; //Steps i hvor smooth linjen skal være.

    [SerializeField] float splineHeight = 5f; //plassere linjen over bakken

    // private List<Vector3> ControlPoints; // List to store control points
    private bool BallStoppedSliding;

    #endregion

    #region Storm

    [SerializeField] private bool shouldMove = true;
    [SerializeField] private List<Vector3> RainPositions = new List<Vector3>(); // lagerer posisjonene til ballen 
    [SerializeField] private int _degree = 2; // graden til splinen
    [SerializeField] private float[] Knots; // Knots array

    #endregion

    //
    [SerializeField] private int _currentIndex; //( nåvarande trekant)
    [SerializeField] private int _previousIndex; //(sist trekant)
    [SerializeField] private Vector3 _previousNormal;
    [SerializeField] private Vector3 _currentNormal;
    private bool isFalling = true;

    private readonly Vector3 gravity = new Vector3(0, -9.81f, 0); // tyngdekraften


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
            ApplyCustomGravity(); // får ballen til å falle

            if (IsCloseToMesh()) // sjekker om ballen har truffet meshen
            {
                isFalling = false;
                _currentfPosition = transform.position;
                _previousPosition = _currentfPosition;
                //lagre posisjonen til ballen når den treffer meshen sånn at det blir korrekt bevegelse    


                _vassDragTimer = 0;
                if (createTrail) // sjekker om vi skal ha trail på ballen, når det er regn eller at det er togglet på så skal det ble laget en linje som viser hvor ballen har vært.
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
        else if (mesh) // sjekker om meshen er laget
        {
            if (shouldMove) // sjekker om ballen skal bevege seg siden vi har en toggle for å stoppe ballen 
            {
                Correction();
                Move();
                if (Acceleration.magnitude <
                    accelerationThreshold) // her har vi forskjellige måter å sjekke om ballen har stoppet eller ikke
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


            // vist ballen ikke skal bevege seg så skal den ikke gjøre noe.
            if (!shouldMove)
            {
                _currentVelocity = Vector3.zero; 
                Acceleration = Vector3.zero; 
                _previousVelocity = Vector3.zero; 
                _previousPosition = _currentfPosition; 
            }


            // VassDrag, her teller vi ned å lagrer rainBall posisjonen  etter et vist tidsintervall. sånn at jeg kan lage en spline som viser hvor ballen har vært.
            _vassDragTimer -= Time.deltaTime;
            if (_vassDragTimer <= 0 && BallStoppedSliding == false)
            {
                _vassDragTimer = _vassDragDT;
                RainPositions.Add(_currentfPosition);

                if (RainPositions.Count > 2) //Når vi har 3 eller flere posisjoner så kan vi lage en spline
                {
                    //kalkulerer knutepunkter
                    CalculateKnots();
                    //oppdaterer linjen  
                    UpdateSplineRenderer();
                }
            }
        }
    }


    private void Awake()
    {
        mesh = FindObjectOfType<MeshGenerator>(); // finner meshen i scenen
    }


    private void Correction()
    {
        Debug.Log("Entering debug log");

        // finner punktet på bakken rett under sentrum av ballen.
        var p = new Vector3(_currentfPosition.x,
            GetValidSurfaceHeight(new Vector3(_currentfPosition.x, 0, _currentfPosition.z)),
            _currentfPosition.z);

        //distanse vektor fra sentrum til p
        var dist = _currentfPosition - p;

        //distansvektoren prosjektert på normalen
        var b = Vector3.Dot(dist, _currentNormal) * _currentNormal;

        if (b.magnitude <= _radius) // sjekker om ballen er under bakken
        {
            Debug.Log("Actually correcting!");

            _currentfPosition = p + _radius * _currentNormal;
            transform.position = _currentfPosition;
        }
        else
        {
            // dette er en workaround for at ballen noen ganger faller gjennom meshen
            float surfaceHeight = GetValidSurfaceHeight(new Vector3(_currentfPosition.x, 0, _currentfPosition.z));
            if (_currentfPosition.y < surfaceHeight)
            {
                _currentfPosition.y = surfaceHeight + _radius; // plasserer ballen på bakken
                _previousPosition = _currentfPosition;
                transform.position = _currentfPosition;
            }
        }
    }


    private void Move()
    {
        //itrerer gjennom hver trekant
        for (var i = 0; i < mesh.triangles.Length; i += 3)
        {
            //skaff hjørnene til trekanten
            var p0 = mesh.vertices[mesh.triangles[i]];
            var p1 = mesh.vertices[mesh.triangles[i + 1]];
            var p2 = mesh.vertices[mesh.triangles[i + 2]];

            // lagre ballens posisjon i xz planet
            var pos = new Vector2(_currentfPosition.x, _currentfPosition.z);

            //finn hvilken trekant ballen er på med barycentric coordinates
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

//Følgende algoritme kan brukes til å nne μ dersom vi har interpolasjon i endene
    // (d+1 skjøter i hver ende av skjøtvektoren, kalles clamped kurve)
    int FindKnotInterval(float x)
    {
        int my = RainPositions.Count - 1;
        while (x < Knots[my])
            my--;
        return my;
    }

    //Algoritmen nedenfor returnerer en tredimensjonal vektor for gitt parameterverdi
    //og er altså for en splinekurve i R^3
    // (dvs. en kurve i rommet). Algoritmen er enkel å utvide til høyere dimensjoner.
    public Vector3 EvaluateBSplineSimple(float x)
    {
        int my = FindKnotInterval(x);

        List<Vector3> a = new List<Vector3>(_degree + 1);
        for (int i = 0; i <= _degree * 4; i++)
        {
            a.Add(Vector3.zero);
        }

        for (int i = 0; i <= _degree; i++)
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
    }

// kalkulerer knutepunkter basert på antall posisjoner og graden
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
                Knots[i] = 0; // knotene fra 0 til d er satt til 0
            }
            else if (i > n)
            {
                Knots[i] = 1; // knotene etter sist kontrollpunkt er satt til 1
            }
            else
            {
                Knots[i] = (i - d) / (float)(n - d); // intern knutepunk kalulering
            }
        }
    }

    private float GetValidSurfaceHeight(Vector3 point)
    {
        // prøver å finne en gyldig overflatehøyde
        float surfaceHeight = mesh.GetSurfaceHeight(new Vector2(point.x, point.z));
        int maxAttempts = 10; // hvor mange ganger vi skal prøve å finne en gyldig høyde

        while (surfaceHeight <= 0 && maxAttempts > 0)
        {
            // justerer punktet litt og prøver igjen
            point += new Vector3(0.1f, 0.1f, 0.1f); // Juster denne verdien etter behov
            surfaceHeight = mesh.GetSurfaceHeight(new Vector2(point.x, point.z));
            maxAttempts--;
        }

        // returner enten den funnet overflatehøyden eller en standardverdi
        return surfaceHeight > 0 ? surfaceHeight + _radius : 0.0f;
    }


    private void BallStopped()
    {
        shouldMove = false;
        BallStoppedSliding = true;
    }

    private void ApplyCustomGravity()
    {
        //er bare for å få ballen til å falle
        _currentVelocity += gravity * Time.fixedDeltaTime;
        _currentfPosition += _currentVelocity * Time.fixedDeltaTime;

        transform.position = _currentfPosition;
    }

    private bool IsCloseToMesh()
    {
        // skaffer høyden til meshen rett under ballen
        Vector2 currentPositionXZ = new Vector2(_currentfPosition.x, _currentfPosition.z);
        float surfaceHeight = mesh.GetSurfaceHeight(currentPositionXZ);


        // sjekk om ballens vertikale posisjon er nær nok til overflaten av den genererte meshen
        return _currentfPosition.y <= surfaceHeight + _radius;
    }


    void UpdateSplineRenderer()
    {
        int numberOfPoints = _steps; // skifter antall punkter på linjen for å få den så smooth som mulig
        splineRenderer.positionCount = numberOfPoints;

        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = i / (float)(numberOfPoints - 1);

            Vector3 pointOnSpline = EvaluateBSplineSimple(t);
            pointOnSpline.y += splineHeight; // legger til en høyde på linjen sånn at den ikke er i bakken

            // garanterer at den riktige posisjonen blir oppdatert
            splineRenderer.SetPosition(i, pointOnSpline);
        }
    }
}