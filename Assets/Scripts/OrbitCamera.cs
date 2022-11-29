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

    //ajuste suave de la rotacion de la camara al seguir al jugador
    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    //radio de seguimiento de la camara, para que la camara no sea tan exacta/estricta al seguir la esfera
    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    //retraso al ajuste automatico de la camara para ajustarse detras del jugador
    [SerializeField, Min(0f)]
    float alignDelay = 5f;

    //posicion del objetivo a seguir, posicion anterior del objetivo a seguir
    Vector3 focusPoint, previousFocusPoint;

    //angulos de orbita, donde X (es vertical, girando hacia abajo) e Y (es horizontal, mirando a Z)
    Vector2 orbitAngles = new Vector2(45f, 0f);

    //ultima vez que se roto manualmente
    float lastManualRotationTime;
    
    //referencia al componente de camara regular
    Camera regularCamera;

    void Awake()
    {
        //obtener la camara al despertar
        regularCamera = GetComponent<Camera>();

        //despertar viendo a la esfera
        focusPoint = focus.position;

        //al despertar mantener los angulos correctos
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

        //rotar camara, si hay cambio manual o automatico...
        if(ManualRotation() || AutomaticRotation()){

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


        //castea una caja para reducir la distancia de enfoque a la esfera si hay un objeto que impacta con la camara. Evita que la camara pase de forma transparente por objetos y achica la vision sobre la esfera.
        if(Physics.BoxCast(
            focusPoint, CameraHalfExtends, -lookDirection, out RaycastHit hit,lookRotation, distance
            )){
                lookPosition = focusPoint - lookDirection * hit.distance;
            }

        //mover camara: posicion y rotacion usando los valores redefinidos anteriormente
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    //actualizar enfoque de la camara
    void UpdateFocusPoint()
    {
        //anterior posicion del objetivo es mi actual
        previousFocusPoint = focusPoint;

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

            //tiempo desde la ultima rotacion sin depender del tiempo en juego
            lastManualRotationTime = Time.unscaledDeltaTime;

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

    //retraso al ajuste automatico
    bool AutomaticRotation()
    {
        //si el tiempo pasado desde la ultima rotacion es menor al tiempo de ajuste automatico
        if(Time.unscaledTime - lastManualRotationTime < alignDelay){

            //no ajustar
            return false;
        }

        //obtener cambio en el movimiento del cuadro
        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z
        );

        //cuadrado del movimiento del cuadro
        float movementDeltaSqr = movement.sqrMagnitude;

        //si el valor anterior es menor a...
        if(movementDeltaSqr < 0.0001f){
            
            //no ajustar
            return false;
        }

        //angulo del enfoque a seguir (objetivo), se pasa el angulo normalizado
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));

        //obtener la diferencia entre angulos de enfoque actual y anterior absoluto (sin signo)
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));

        //cambio de rotacion. aumentara la sensibilidad del cambio para que no sea brusco usando el minimo del tiempo sin escalar en juego o el cuadrado del movimiento del cuadro
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        //si la diferencia absoluta del angulo de enfoque es menor al rango de ajuste suave...
        if(deltaAbs < alignSmoothRange){

            //multiplicar el resultado de la division por rotacionChange y guardarlo en rotacionChange (operador multiplyAND). Escalar el cambio de rotacion acorde a su angulo de enfoque y el ajuste suave
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        //cuando el angulo pasa de los 180... Escala el cambio de rotacion cuando la esfera va hacia a la camara
        else if(180f - deltaAbs < alignSmoothRange){
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }

        //el nuevo angulo sera mi nuevo enfoque limitado entre 0 y 360 grados
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);

        //ajustar
        return true;
    }

    //encontrar el angulo horizontal que encaja con la direccion del objetivo
    static float GetAngle(Vector2 direction)
    {
        //obtener angulo segun una direccion en Y
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        
        //el angulo podria rotar a sentido horario o antihorario, entonces se revisara su X para saber cual sentido es. Si es menor a 0f, restar 360f. Sino retornar el angulo sin modificar
        return direction.x < 0f ? 360f - angle : angle;
    }

    //se obtiene la mitad de las extensiones de una caja (box cast). Para hacer la camara ocupar un espacio similar a un rectangulo. Esto permitira que el movimiento de la camara a traves de obstaculos sea mas real. Como las camaras reales que tratan de enfocar lo mas cercano a ellas y lo demas queda difuso
    Vector3 CameraHalfExtends
    {
        get
        {
            //obtener la mitad de las extensiones del rectangulo
            Vector3 halfExtends;

            //en Y (altura del rectangulo): plano cercano de la camara por la tangente de la mitad del campo de vision de la camara pasada a radianes
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);

            //en X (ancho del rectangulo)
            halfExtends.x = halfExtends.y * regularCamera.aspect;

            //en Z (profundidad del rectangulo)
            halfExtends.z = 0f;
            return halfExtends;
        }
    }
}
