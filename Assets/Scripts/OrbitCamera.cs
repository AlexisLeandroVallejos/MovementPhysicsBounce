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

    //actualizar tardio para seguir con la camera luego de que Update() pase
    void LateUpdate()
    {
        //lugar/cosa a enfocar
        Vector3 focusPoint = focus.position;

        //donde apuntar la camara
        Vector3 lookDirection = transform.forward;

        //mover camara: posicion de la esfera - posicion de la camara por su distancia
        transform.localPosition = focusPoint - lookDirection * distance;
    }
}
