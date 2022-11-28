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

    //radio de seguimiento de la camara, para que la camara no sea tan exacta/estricta al seguir la esfera
    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    //posicion del objetivo a seguir
    Vector3 focusPoint;

    void Awake()
    {
        //despertar viendo a la esfera
        focusPoint = focus.position;
    }

    //actualizar tardio para seguir con la camera luego de que Update() pase
    void LateUpdate()
    {
        UpdateFocusPoint();

        //donde apuntar la camara
        Vector3 lookDirection = transform.forward;

        //mover camara: posicion de la esfera - posicion de la camara por su distancia
        transform.localPosition = focusPoint - lookDirection * distance;
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
}
