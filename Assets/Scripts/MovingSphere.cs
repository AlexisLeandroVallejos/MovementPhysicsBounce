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
    
    //guardo valor de velocidad
    Vector3 velocity;

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

        //modificar a velocidadDeseada aplicando velocidad+aceleracion
        Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        //cambio de velocidad sera maxAceleracion * tiempo
        float maxSpeedChange = maxAcceleration * Time.deltaTime;

        //MoveTowards para reemplazar condiciones if, dado un valor actual, valor deseado y la diferencia maxima
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        //usar movimiento relativo para evitar un movimiento de teleporte
        Vector3 displacement = velocity * Time.deltaTime;
        
        //posicionamiento local de la esfera previamente iniciada
        Vector3 newPosition = transform.localPosition + displacement;

        transform.localPosition = newPosition;
    }
}
