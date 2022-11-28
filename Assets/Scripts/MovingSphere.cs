using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    //campo de velocidad para la esfera
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    //campo de aceleracion para la esfera, campo de aceleracion en aire
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    //campo de altura de salto
    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;
    
    //guardo valor de velocidad y velocidad deseada
    Vector3 velocity, desiredVelocity;

    //controlar el rigidbody
    Rigidbody body;

    //saltara
    bool desiredJump;

    //saltos aereos
    [SerializeField, Range(0,5)]
    int maxAirJumps = 0;

    //conteo de saltos
    int jumpPhase;

    //maximo angulo de contacto con el piso
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    //obtener producto escalar para calcular la normal en inclinacion usando coseno
    float minGroundDotProduct;

    //campo contacto normal para saltar como fisicamente es correcto y no directamente hacia arriba
    Vector3 contactNormal;

    //contador de contactos con el piso
    int groundContactCount;

    //si hay mas de un contacto OnGround sera true
    bool OnGround => groundContactCount > 0;

    //pasos fisicos desde que toco piso, agregado pasos desde el ultimo salto
    int stepsSinceLastGrounded, stepsSinceLastJump;

    //velocidad de pegado
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;

    //distancia de sondeo
    [SerializeField, Min(0f)]
    float probeDistance = 1f;

    //mascara de capas, en unity va a sondear todo excepto Agent(otras esferas) y Ignore Raycast
    [SerializeField]
    LayerMask probeMask = -1;

    //se mantendra sincronizado mientras este en play mode
    void OnValidate(){
        //Mathf.Cos espera radianes
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void Awake() 
    {
        body = GetComponent<Rigidbody>();
        //se agrega para que tambien se sincronice en builds
        OnValidate();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //inicio el playerinput
        Vector2 playerInput;

        //obtengo los valores del input, los asigno al playerinput
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        
        //restringir playerInput para un mejor control dentro del circulo
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        //velocidad deseada es un campo para usar en FixedUpdate
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        //saltar al presionar Espacio, "OR asignado" para que siempre sea true hasta modificarlo
        desiredJump |= Input.GetButtonDown("Jump");

        //ver la cantidad de contactos, cuanto mas contactos mas claro sera el color
        /*
        GetComponent<Renderer>().material.SetColor(
            "_Color", Color.white * (groundContactCount * 0.25f)
        );
        */
        //ver si esta en contacto con el piso
        GetComponent<Renderer>().material.SetColor(
            "_Color", OnGround ? Color.black : Color.white
        );
    }

    //PhysX ejecuta esto primero, despues las colisiones
    void FixedUpdate()
    {
        UpdateState();
        
        //ajustar velocidades en pendiente y mantener una correcta relacion con la velocidad en piso
        AdjustVelocity();

        //si es salto deseado
        if(desiredJump){
            desiredJump = false;
            Jump();
        }

        //aplicar al rigidbody la velocidad arbitraria
        body.velocity = velocity;

        //resetear estado de los contactos
        ClearState();
    }

    void ClearState()
    {
        //resetear contador de contactos
        groundContactCount = 0;
        
        //vectores normales dejarlos en cero
        contactNormal = Vector3.zero;
    }

    void UpdateState()
    {
        //aumentar pasos fisicos desde la ultima vez que toco piso la esfera
        stepsSinceLastGrounded += 1;

        //aumentar pasos fisicos desde el ultimo salto
        stepsSinceLastJump += 1;

        //conseguir velocidad del rigidbody antes de manipularla
        velocity = body.velocity;

        //si esta en el piso o esta pegado a el
        if(OnGround || SnapToGround()){
            //resetear pasos, porque ya esta en contacto con el piso
            stepsSinceLastGrounded = 0;

            jumpPhase = 0;

            //solamente normalizar si hay mas de un contacto
            if(groundContactCount > 1){
                //normalizar salto en piso
                contactNormal.Normalize();
            }
        }
        //saltos aeros todavia serviran
        else{
            contactNormal = Vector3.up;
        }
    }

    void Jump()
    {
        //saltar solo en contacto y mientras sea menor a los saltos aeros permitidos
        if(OnGround || jumpPhase < maxAirJumps){
            //resetear contador de pasos fisicos desde el ultimo salto
            stepsSinceLastJump = 0;

            //sumar saltos
            jumpPhase += 1;

            //recalcular y separar para evitar velocidad excesiva en salto aereo
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            //velocidad alineada por su contacto normal y velocidad usando producto escalar
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);

            //el vector de velocidad ahora dependera del producto escalar: velocidad y contacto normal
            if(alignedSpeed > 0f){
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }

            //calculo mas directo sobre el vector
            velocity += contactNormal * jumpSpeed;
        }
        
    }

    //saltar solo en contacto con piso
    void OnCollisionEnter(Collision collision)
    {
        //evaluar colision pared/piso
        EvaluateCollision(collision);
    }

    //saltar mientras exista contacto con piso
    void OnCollisionStay(Collision collision)
    {
        //evaluar colision pared/piso
        EvaluateCollision(collision);
    }

    //verificar la colision de la esfera
    void EvaluateCollision(Collision collision)
    {
        //usar la normal para obtener el contacto con algo
        for (int i = 0; i < collision.contactCount; i++){
            Vector3 normal = collision.GetContact(i).normal;

            //calculo que estoy saltando correctamente cuando hago contacto con el piso
            if(normal.y >= minGroundDotProduct){
                
                //aumentar cantidad de contactos
                groundContactCount += 1;

                //acumular normales
                contactNormal += normal;
            }
        }
    }

    //alinear velocidad deseada con el piso
    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    void AdjustVelocity()
    {
        //vectores de velocidad alineados con el piso para mantener una velocidad correcta en pendientes
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        //proyectar velocidad actual y nueva velocidad con respecto al piso
        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);
        
        //calcular la diferencia de velocidad entre la vieja y la nueva
        velocity += xAxis * (newX - currentX) + zAxis * (newZ- currentZ);
    }

    //pegar la esfera al piso
    bool SnapToGround()
    {
        //si la esfera esta volando o hay saltos aeros permitidos, no podra pegarse al piso
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2){
            return false;
        }

        //velocidad-magnitud de la esfera, movido por la velocidad de pegado para volver false si la velocidad-magnitud es superior a la maxima de pegado
        float speed = velocity.magnitude;
        if(speed > maxSnapSpeed){
            return false;
        }

        //si no hay piso detectado por el raycast, no se podra pegar al piso, agregada distancia de sondeo, agregada mascara para ignorar ciertas capas
        if( !Physics.Raycast(
            body.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask)){
            return false;
        }

        //Verificar hit si pego piso, sino devolver falso
        if(hit.normal.y < minGroundDotProduct){
            return false;
        }

        //pegar al piso cuando todas las anteriores condiciones no se cumplan
        groundContactCount = 1;
        //usar el hit del raycast para que sea nuestra nueva normal de contacto
        contactNormal = hit.normal;
        
        
        //producto escalar de la velocidad y la normal del hit raycast
        float dot = Vector3.Dot(velocity, hit.normal);
        //ajustar velocidad cuando el producto escalar y la normal de la superficie sean superiores a 0.
        if(dot > 0f){
            //recalculo la velocidad como la diferencia de la velocidad y la normal del hit por su producto escalar. Normalizo y multiplico por velocidad-magnitud: reajustar al piso
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }
}
