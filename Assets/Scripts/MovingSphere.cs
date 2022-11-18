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

    //en contacto con el suelo para evitar salto infinito
    bool onGround;

    //saltos aereos
    [SerializeField, Range(0,5)]
    int maxAirJumps = 0;

    //conteo de saltos
    int jumpPhase;

    void Awake() 
    {
        body = GetComponent<Rigidbody>();
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
    }

    //PhysX ejecuta esto primero, despues las colisiones
    void FixedUpdate()
    {
        UpdateState();

        //campo de aceleracion dependiendo de estar en el piso o en el aire
        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;

        //cambio de velocidad sera maxAceleracion * tiempo
        float maxSpeedChange = acceleration * Time.deltaTime;

        //MoveTowards para reemplazar condiciones if, dado un valor actual, valor deseado y la diferencia maxima
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        //si es salto deseado
        if(desiredJump){
            desiredJump = false;
            Jump();
        }

        //aplicar al rigidbody la velocidad arbitraria
        body.velocity = velocity;

        //setear a false por el momento y dejar que las colisiones se encarguen del bool
        onGround = false;
    }

    void UpdateState()
    {
        //conseguir velocidad del rigidbody antes de manipularla
        velocity = body.velocity;

        //reiniciar conteo de saltos
        if(onGround){
            jumpPhase = 0;
        }
    }

    void Jump()
    {
        //saltar solo en contacto y mientras sea menor a los saltos aeros permitidos
        if(onGround || jumpPhase < maxAirJumps){
            //sumar saltos
            jumpPhase += 1;

            //recalcular y separar para evitar velocidad excesiva en salto aereo
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            if(velocity.y > 0f){
                //evitar que el salto sea negativo cuando este cayendo (impulsar un empuje hacia abajo)
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }
            velocity.y += jumpSpeed;
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
            //booleano mientras el normal sea 0.9 o superior para el salto
            onGround |= normal.y >=0.9f;
        }
    }
}
