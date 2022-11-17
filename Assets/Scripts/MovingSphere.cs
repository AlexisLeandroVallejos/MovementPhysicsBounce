using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    //campo de velocidad para la esfera
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    //campo de aceleracion para la esfera
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;

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

    void FixedUpdate()
    {
        //conseguir velocidad del rigidbody antes de manipularla
        velocity = body.velocity;

        //cambio de velocidad sera maxAceleracion * tiempo
        float maxSpeedChange = maxAcceleration * Time.deltaTime;

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
    }

    void Jump()
    {
        //saltar solo en contacto
        if(onGround){
            //calculo debido a gravedad
            velocity.y += Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
        }
        
    }

    //saltar solo en contacto con algo (no especifica piso)
    void OnCollisionEnter()
    {
        onGround = true;
    }

    void OnCollisionExit()
    {
        onGround = false;
    }
}
