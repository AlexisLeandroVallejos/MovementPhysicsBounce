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

    //saltos aereos
    [SerializeField, Range(0,5)]
    int maxAirJumps = 0;

    //maximo angulo de contacto con el piso, agregado angulo de escalera
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f, maxStairsAngle = 50f;

    //velocidad de pegado
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;

    //distancia de sondeo
    [SerializeField, Min(0f)]
    float probeDistance = 1f;

    //mascara de capas, en unity va a sondear todo excepto Agent(otras esferas) y Ignore Raycast(mascara de sondeo), agregada capa/mascara escalera
    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;

    //espacio de entrada del jugador
    [SerializeField]
    Transform playerInputSpace = default;

    //guardo valor de velocidad y velocidad deseada
    Vector3 velocity, desiredVelocity;

    //controlar el rigidbody
    Rigidbody body;

    //saltara
    bool desiredJump;

    //conteo de saltos
    int jumpPhase;

    //obtener producto escalar para calcular la normal en inclinacion usando coseno, agregado producto escalar para escaleras
    float minGroundDotProduct, minStairsDotProduct;

    //campo contacto normal para saltar como fisicamente es correcto y no directamente hacia arriba, agregado normal de terreno escarpado/empinado
    Vector3 contactNormal, steepNormal;

    //contador de contactos con el piso
    int groundContactCount, steepContactCount;

    //si hay mas de un contacto con el piso, OnGround sera true
    bool OnGround => groundContactCount > 0;

    //si hay mas de un contacto escarpado, OnSteep sera true
    bool OnSteep => steepContactCount > 0;

    //pasos fisicos desde que toco piso, agregado pasos desde el ultimo salto
    int stepsSinceLastGrounded, stepsSinceLastJump;

    //eje superior o hacia arriba
    Vector3 upAxis;

    //se mantendra sincronizado mientras este en play mode
    void OnValidate(){
        //Mathf.Cos espera radianes, en contacto con piso
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        
        //calculo que pasa con escaleras
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
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

        //si hay espacio de entrada del jugador...
        if(playerInputSpace){

            //normalizar forward y right (XZ) para que la camara no afecte la velocidad deseada de la esfera al rotar su orbita vertical
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            
            //la velocidad deseada sera modificada segun este espacio
            desiredVelocity = (forward * playerInput.y + right * playerInput.x) * maxSpeed;
        }
        //sino usarla directamente de los controles de entrada
        else{
            //velocidad deseada es un campo para usar en FixedUpdate
            desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        }

        //saltar al presionar Espacio, "OR asignado" para que siempre sea true hasta modificarlo
        desiredJump |= Input.GetButtonDown("Jump");

    }

    //PhysX ejecuta esto primero, despues las colisiones
    void FixedUpdate()
    {
        //direccion opuesta de la gravedad
        upAxis = -Physics.gravity.normalized;

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
        //resetear contador de contactos: pisos y escarpados
        groundContactCount = steepContactCount = 0;
        
        //vectores normales dejarlos en cero: pisos y escarpados
        contactNormal = steepNormal = Vector3.zero;
    }

    void UpdateState()
    {
        //aumentar pasos fisicos desde la ultima vez que toco piso la esfera
        stepsSinceLastGrounded += 1;
        
        //aumentar pasos fisicos desde el ultimo salto
        stepsSinceLastJump += 1;

        //conseguir velocidad del rigidbody antes de manipularla
        velocity = body.velocity;

        //si esta en el piso o esta pegado a el o es un contacto escarpado
        if(OnGround || SnapToGround() || CheckSteepContacts()){

            //resetear pasos, porque ya esta en contacto con el piso
            stepsSinceLastGrounded = 0;

            //solo resetear la fase si hay mas de un paso desde el ultimo salto
            if(stepsSinceLastJump > 1){
                //fase del salto (contador de saltos aeros)
                jumpPhase = 0;
            }

            //solamente normalizar si hay mas de un contacto
            if(groundContactCount > 1){
                //normalizar salto en piso
                contactNormal.Normalize();
            }
        }
        //saltos aeros todavia serviran
        else{
            //eje hacia arriba
            contactNormal = upAxis;
        }
    }

    void Jump()
    {
        //direccion de salto
        Vector3 jumpDirection;

        //si esta en el piso
        if(OnGround){
            
            //la direccion del salto es la del contacto normal con el piso
            jumpDirection = contactNormal;
        }
        //si esta en escarpado
        else if(OnSteep){

            //la direccion del salto es la del contacto normal con el escarpado
            jumpDirection = steepNormal;

            //resetear la fase del salto para permitir salto aereo cuando se esta en contacto con terreno escarpado
            jumpPhase = 0;
        }
        //si aun hay saltos aeros disponibles. agregada condicion para evitar salto aereo extra con maxAirJumps y su fase de salto
        else if(maxAirJumps > 0 && jumpPhase <= maxAirJumps){
            
            //debido a los ajustes anteriores, va a haber un posible salto aereo extra. Se ajusta eso aqui para que no suceda
            if(jumpPhase == 0){
                jumpPhase = 1;
            }

            //la direccion del salto es la del contacto normal con el piso
            jumpDirection = contactNormal;
        }
        //sino
        else{
            //no es posible saltar
            return;
        }

        //resetear contador de pasos fisicos desde el ultimo salto
        stepsSinceLastJump = 0;

        //sumar saltos
        jumpPhase += 1;

        //recalcular y separar para evitar velocidad excesiva en salto aereo. Reemplazo ecuaciones en casos de gravedades diferentes
        float jumpSpeed = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);

        //sesgo de salto ascendente, para hacer que un salto pueda aumentar su velocidad en Y (impulsarse hacia arriba con otro salto despues de tocar pared). En superficie plana, no se percibe este cambio, solo en encarpada. Reemplazo con eje hacia arriba (gravedad diferente)
        jumpDirection = (jumpDirection + upAxis).normalized;

        //velocidad alineada por velocidad de la esfera y direccion de salto
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);

        //el vector de velocidad ahora dependera del producto escalar: velocidad y contacto normal
        if(alignedSpeed > 0f){
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }

        //velocidad de la esfera es direccion de salto * velocidad de salto
        velocity += jumpDirection * jumpSpeed;
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
        //obtener el producto escalar correcto, y evaluar colision con el
        float minDot = GetMinDot(collision.gameObject.layer);

        //usar la normal para obtener el contacto con algo
        for (int i = 0; i < collision.contactCount; i++){

            //obtener la normal de la colision
            Vector3 normal = collision.GetContact(i).normal;

            //producto escalar del eje hacia arriba y la normal
            float upDot = Vector3.Dot(upAxis, normal);

            //calcular contacto con el minimo del producto escalar sea piso/escalera. Reemplazo con producto escalar del eje hacia arriba
            if(upDot >= minDot){
                
                //aumentar cantidad de contactos
                groundContactCount += 1;

                //acumular normales
                contactNormal += normal;
            }
            //sino hay contacto con el piso, chequear contacto empinado. Producto escalar de esto deberia ser cero, por las dudas se usa -0.01f. Reemplazo con producto escalar del eje hacia arriba
            else if(upDot > -0.01f){
                steepContactCount += 1;
                steepNormal += normal;
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

        //si no hay piso detectado por el raycast, no se podra pegar al piso, agregada distancia de sondeo, agregada mascara para ignorar ciertas capas. Reemplazo con upAxis negado
        if( !Physics.Raycast(
            body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask)){
            return false;
        }

        //producto escalar del eje hacia arriba y la normal
        float upDot = Vector3.Dot(upAxis, hit.normal);

        //Verificar hit si pego piso/escalera, sino devolver falso
        if(upDot < GetMinDot(hit.collider.gameObject.layer)){
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

    //devolver minimo producto escalar apropiado entre piso o escalera (minground/minStairs valor)
    float GetMinDot(int layer)
    {
        //bit mask(mascara binaria), soportar cualquier tipo de capa:
        return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
    }

    //convertir contactos escarpados en un piso virtual para poder moverse incluso cuando se podria hacer fisicamente (saltar pared y rebotar/impulsarse en ellas, etc)
    bool CheckSteepContacts()
    {
        //si hay mas de un contacto escarpado
        if(steepContactCount > 1){
            
            //normalizar el contacto
            steepNormal.Normalize();

            //producto escalar del eje superior y la normal escarpada
            float upDot = Vector3.Dot(upAxis, steepNormal);

            //si el contacto escarpado es mayor o igual al producto escalar minimo en el piso...
            if(upDot >= minGroundDotProduct){
                
                //el contacto escarpado es un piso virtual
                groundContactCount = 1;

                //contacto escarpado es igual a un contacto en el piso
                contactNormal = steepNormal;

                //es un contacto escarpado en el que puede moverse
                return true;
            }
        }
        //NO es un contacto escarpado en el que puede moverse
        return false;
    }
}