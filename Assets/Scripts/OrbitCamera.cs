using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour 
{
    //objetivo a seguir
    [SerializeField]
    Transform focus = default;

    //distancia al objetivo a seguir
    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    //retraso al enfoque centrado, para que la camara tarde en centrarse en el objetivo y se siga moviendo
    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    //velocidad de rotacion de la orbita de la camara
    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    //limitar angulos de la camara para que no vuelva complicada para la vista
    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    //radio de seguimiento de la camara, para que la camara no sea tan exacta/estricta al seguir la esfera
    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    //posicion del objetivo a seguir
    Vector3 focusPoint;

    //angulos de orbita, donde X (es vertical, girando hacia abajo) e Y (es horizontal, mirando a Z)
    Vector2 orbitAngles = new Vector2(45f, 0f);

    void Awake()
    {
        //despertar viendo a la esfera
        focusPoint = focus.position;

        //al iniciar mantener los angulos correctos
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }


    void OnValidate()
    {
        //asegurarse que el maximo nunca sea menor que el minimo en el inspector de unity
        if(maxVerticalAngle < minVerticalAngle){
            maxVerticalAngle = minVerticalAngle;
        }
    }

    //actualizar tardio para seguir con la camera luego de que Update() pase
    void LateUpdate()
    {
        //actualizar enfoque
        UpdateFocusPoint();

        //rotacion de vista
        Quaternion lookRotation;

        //rotar camara, si hay cambio...
        if(ManualRotation()){

            //limitar angulos
            ConstrainAngles();

            //obtener rotacion de vista ajustada
            lookRotation = Quaternion.Euler(orbitAngles);
        }
        else{
            //sino obtener rotacion de vista sin ajustar porque no hubo cambio
            lookRotation = transform.localRotation;
        }

        //donde apuntar la camara, con los angulos de orbita de la camara
        Vector3 lookDirection = lookRotation * Vector3.forward;

        //mover posicion de la camara: posicion de la esfera - posicion de la camara por su distancia
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        //mover camara: posicion y rotacion usando los valores redefinidos anteriormente
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    //actualizar enfoque de la camara
    void UpdateFocusPoint()
    {
        //objetivo es posicion de la esfera
        Vector3 targetPoint = focus.position;

        //si el radio de enfoque
        if(focusRadius > 0f){

            //distancia entre el objetivo(esfera) y el enfoque actual
            float distance = Vector3.Distance(targetPoint, focusPoint);

            //potencia para la interpolacion
            float t = 1f;

            //si la distancia es mayor a 0.01f y el enfoque centrado es mayor a 0f...
            if(distance > 0.01f && focusCentering > 0f){

                //calcula la potencia bajo la formula (1-c)^t para interpolar y usar el retraso de enfoque centrado. Usar unscaled para evitar que la camara se congele o ande lento por efectos especiales (slow motion, pausa en juego, etc) 
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            //si la distancia es mayor al radio de enfoque de la camara...
            if(distance > focusRadius){

                //calcula el minimo dada la potencia y la interpolacion distancia-radio enfoque
                t = Mathf.Min(t, focusRadius/distance);
            }
            
            //nuevo punto de enfoque sera el objetivo y el enfoque anterior con potencia t como interpolador linear (creador de nuevos puntos a partir de viejos)
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else{
            //la posicion del objetivo a seguir es la nueva posicion de mi objetivo
            focusPoint = targetPoint;
        }

        
    }

    //rotacion de la orbita de la camara, solo activo cuando cambia
    bool ManualRotation()
    {
        //controles de la camara vertical y horizontal
        Vector2 input = new Vector2(
            Input.GetAxis("Vertical Camera"), 
            Input.GetAxis("Horizontal Camera")
        );

        //epsilon
        const float e = 0.001f;

        //si alguna entrada es menor o mayor a -/+epsilon...
        if(input.x < -e || input.x > e || input.y < -e || input.y > e){
            
            //agregar la entrada a los angulos de orbita de la camara, escalada por la velocidad de rotacion y el tiempo delta independiente del tiempo en juego
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;

            //cambio el angulo, entonces es true
            return true;
        }

        //no hubo cambio
        return false;
    }

    //restringir angulos de la camara
    void ConstrainAngles()
    {
        //restringir angulo vertical entre el min y max permitido
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);
        
        //restringir angulo horizontal, mantiene el angulo entre 0f y 360f
        if(orbitAngles.y < 0f){
            orbitAngles.y += 360f;
        }
        else if(orbitAngles.y >= 360f){
            orbitAngles.y -= 360f;
        }
    }
}
