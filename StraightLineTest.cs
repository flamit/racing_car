using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

[RequireComponent(typeof(Rigidbody))]
public class StraightLineTest : MonoBehaviour {

    //public float engineForce = 10.0f;   // Constant force from the engine
    public float cDrag;                 // Drag (air friction constant)
    public float cRoll;                 // Rolling resistance (wheel friction constant)

    public float carMass = 1200.0f;     // Mass of the car in Kilograms

    Rigidbody rigidbody;

    bool accel;

    //public WheelTest[] wheels;
    public WheelController[] wheels;

    //Engine parameters:
    public float rpmMin = 1000.0f;
    public float rpmMax = 6000.0f;
    public float rpm;
    public float engineTorque;
    public float throttlePos;
    public float maxTorque;
    public float peakTorque  = 100f;
    public AnimationCurve torqueRPMCurve;
    public Transform centerOfMass;
    public float gearRatio = 2.66f; // First gear hardcoded
    public float differentialRatio = 3.42f;
    public float steeringSensitivity = 0.5f;

    public AnimationCurve steeringSensitityCurve;
    public float maxSpeed;

    public float brakingPower = 100f;

	
	[NonSerialized] public bool processContacts = false;		// This is set to True by the components that use contacts (Audio, Damage)
	[NonSerialized] public float impactThreeshold = 0.6f;		// 0.0 - 1.0. The DotNormal of the impact is calculated. Less than this value means drag, more means impact.
	[NonSerialized] public float impactInterval = 0.2f;			// Time interval between processing impacts for visual or sound effects.
	[NonSerialized] public float impactIntervalRandom = 0.4f;	// Random percentage for the impact interval, avoiding regularities.
	[NonSerialized] public float impactMinSpeed = 2.0f;			// Minimum relative velocity at which contacts may be consideered impacts.

	Transform m_transform;
	Rigidbody m_rigidbody;

	public Transform cachedTransform { get { return m_transform; } }
	public Rigidbody cachedRigidbody { get { return m_rigidbody; } }

	public bool showContactGizmos = false;

	Vector3 m_localDragPosition = Vector3.zero;
	Vector3 m_localDragVelocity = Vector3.zero;
	int m_localDragHardness = 0;

	float m_lastStrongImpactTime = 0.0f;

//	[NonSerialized] public bool computeExtendedTireData = false;// Components using extended tire data (tire marks, smoke, particles, audio) set this to True

	public Vector3 localImpactPosition { get { return m_sumImpactPosition; } }
	public Vector3 localImpactVelocity { get { return m_sumImpactVelocity; } }
	public bool isHardDrag { get { return m_localDragHardness >= 0; } }

	int m_sumImpactCount = 0;
	Vector3 m_sumImpactPosition = Vector3.zero;
	Vector3 m_sumImpactVelocity = Vector3.zero;
	int m_sumImpactHardness = 0;
	float m_lastImpactTime = 0.0f;

	public delegate void OnImpact();
	public OnImpact onImpact;

	public static StraightLineTest current = null;

	//--added for impacts on car

	void Start () {
        rigidbody = GetComponent<Rigidbody>();
	}

	void OnEnable ()
	{
		// Cache/find components and configure rigidbody

		m_transform = GetComponent<Transform>();
		m_rigidbody = GetComponent<Rigidbody>();
	}
		
    void Update()
    {
        accel = Input.GetKey(KeyCode.W);
        float throttlePos = Input.GetAxis("Vertical");
        if(Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Brakes");
            wheels[0].brakeTorque = brakingPower;
            wheels[1].brakeTorque = brakingPower;
            wheels[2].brakeTorque = brakingPower;
            wheels[3].brakeTorque = brakingPower;

        }
        else
        {
            wheels[0].brakeTorque = 0.0f;
            wheels[1].brakeTorque = 0.0f;
            wheels[2].brakeTorque = 0.0f;
            wheels[3].brakeTorque = 0.0f;
        }
        float wheelRotRate = 0.5f * (wheels[0].AngularVelocity + wheels[1].AngularVelocity);

        rpm = wheelRotRate * gearRatio * differentialRatio * 60.0f / (2.0f * Mathf.PI);
        rpm = Mathf.Clamp(rpm, rpmMin, rpmMax);

        maxTorque = GetMaxTorque(rpm);
        engineTorque = maxTorque * throttlePos;

        wheels[0].driveTorque = engineTorque;
        wheels[1].driveTorque = engineTorque;

		
		if (processContacts)
		{
			UpdateDragState(Vector3.zero, Vector3.zero, m_localDragHardness);
			// debugText = string.Format("Drag Pos: {0}  Drag Velocity: {1,5:0.00}  Drag Friction: {2,4:0.00}", localDragPosition, localDragVelocity.magnitude, localDragFriction);
		}
		//--abcc
    }

    public float normalizedRPM;
    float GetMaxTorque(float currentRPM)
    {
        normalizedRPM = (currentRPM - rpmMin) / (rpmMax - rpmMin);
        float val = torqueRPMCurve.Evaluate(Mathf.Abs(normalizedRPM)) * Mathf.Sign(normalizedRPM);

        return val * peakTorque;
    }

    Vector3 prevVel, totalAccel;
    float steeringAngle = 0.0f;
    public float carAngularSpeed = 0f;

	// Update is called once per frame
	void FixedUpdate ()
    {
        rigidbody.centerOfMass = centerOfMass.localPosition;
        carAngularSpeed = rigidbody.angularVelocity.y;
        steeringAngle = Input.GetAxis("Horizontal") * steeringSensitivity * steeringSensitityCurve.Evaluate(transform.InverseTransformDirection(rigidbody.velocity).z / maxSpeed) * 45.0f;
        steeringAngle = Mathf.Clamp(steeringAngle, -45.0f, 45.0f);
        //wheels[0].transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * 30.0f - transform.rotation.y, Space.Self);
        //wheels[1].transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * 30.0f - transform.rotation.y, Space.Self);
        //wheels[0].transform.localRotation = Quaternion.Euler(0.0f, steeringAngle, 0.0f);
        //wheels[1].transform.localRotation = Quaternion.Euler(0.0f, steeringAngle, 0.0f);

        wheels[0].steeringAngle = steeringAngle;
        wheels[1].steeringAngle = steeringAngle;

        Vector3 velocity = rigidbody.transform.InverseTransformDirection(rigidbody.velocity);
       // Vector3 fTraction = transform.forward * (accel ? engineForce : 0.0f);
        Vector3 fDrag = -cDrag * velocity.z * velocity.z * transform.forward;
        Vector3 fRoll = -cRoll * velocity.z * transform.forward;

        Vector3 fLong = fDrag + fRoll;// +fTraction;  // Total longitudinal force

        Vector3 acceleration = fLong / carMass;
        Debug.DrawLine(transform.position, transform.position + totalAccel, Color.magenta);
       // Debug.DrawLine(transform.position, transform.position + rigidbody.velocity, Color.cyan);

        rigidbody.velocity += acceleration * Time.deltaTime;//rigidbody.transform.TransformDirection( acceleration) * Time.deltaTime;
        //rigidbody.AddForce( rigidbody.transform.TransformDirection(fLong));

        totalAccel = (rigidbody.velocity - prevVel) / Time.deltaTime;
        totalAccel = totalAccel.magnitude > 15.0f ? totalAccel.normalized : totalAccel;
        rigidbody.centerOfMass -= transform.InverseTransformDirection(Vector3.Scale(totalAccel, Vector3.forward + Vector3.right) * 0.01f);
        //rigidbody.angularDrag = rigidbody.angularVelocity.y * 0.1f;
        prevVel = rigidbody.velocity;
        if (Mathf.Abs(rigidbody.angularVelocity.y) > 5.0f)
            rigidbody.angularDrag = 3.0f;
        else
            rigidbody.angularDrag = 0.1f;

		
		if (processContacts)
			HandleImpacts();
	}


    Rect areagui = new Rect(0f, 0f, 500f, 300f);
    void OnGUI()
    {
        
        GUILayout.BeginArea(areagui, EditorStyles.helpBox);
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.Label("Wheel");
        GUILayout.Label("RPM");
        GUILayout.Label("FroceFWD");
        GUILayout.Label("ForceSide");
        GUILayout.Label("SlipRatio");
        GUILayout.Label("SlipAngle");
        GUILayout.EndVertical();

        foreach(WheelController w in wheels)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(w.name);
            GUILayout.Label(w.rpm.ToString("0.0"));
            GUILayout.Label(w.fwdForce.ToString("0.0"));
            GUILayout.Label(w.sideForce.ToString("0.0"));
            GUILayout.Label(w.slipRatio.ToString("0.0"));
            GUILayout.Label((w.slipAngle).ToString("0.0"));
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawSphere(GetComponent<Rigidbody>().worldCenterOfMass, 0.1f);
    }

	//added by cockcrow
	void OnCollisionEnter (Collision collision)
	{
		// Prevent the wheels to sleep for some time if a strong impact occurs

		if (collision.relativeVelocity.magnitude > 4.0f)
			m_lastStrongImpactTime = Time.time;

		if (processContacts)
			ProcessContacts(collision, true);
	}


	void OnCollisionStay (Collision collision)
	{
		if (processContacts)
			ProcessContacts(collision, false);
	}

	void ProcessContacts (Collision col, bool forceImpact)
	{
		int impactCount = 0;						// All impacts
		Vector3 impactPosition = Vector3.zero;
		Vector3 impactVelocity = Vector3.zero;
		int impactHardness = 0;

		int dragCount = 0;
		Vector3 dragPosition = Vector3.zero;
		Vector3 dragVelocity = Vector3.zero;
		int dragHardness = 0;

		float sqrImpactSpeed = impactMinSpeed*impactMinSpeed;

		// We process all contacts individually and get an impact and/or drag amount out of each one.

		foreach (ContactPoint contact in col.contacts)
		{
			Collider collider = contact.otherCollider;

			// Get the type of the impacted material: hard +1, soft -1

			int hardness = 0;
//			UpdateGroundMaterialCached(collider.sharedMaterial, ref m_lastImpactedMaterial, ref m_impactedGroundMaterial);

//			if (m_impactedGroundMaterial != null)
//				hardness = m_impactedGroundMaterial.surfaceType == GroundMaterial.SurfaceType.Hard? +1 : -1;

			// Calculate the velocity of the body in the contact point with respect to the colliding object

			Vector3 v = m_rigidbody.GetPointVelocity(contact.point);
			if (collider.attachedRigidbody != null)
				v -= collider.attachedRigidbody.GetPointVelocity(contact.point);

			float dragRatio = Vector3.Dot(v, contact.normal);

			// Determine whether this contact is an impact or a drag

			if (dragRatio < -impactThreeshold || forceImpact && col.relativeVelocity.sqrMagnitude > sqrImpactSpeed)
			{
				// Impact

				impactCount++;
				impactPosition += contact.point;
				impactVelocity += col.relativeVelocity;
				impactHardness += hardness;

//				if (showContactGizmos)
//					Debug.DrawLine(contact.point, contact.point + CommonTools.Lin2Log(v), Color.red);
			}
			else if (dragRatio < impactThreeshold)
			{
				// Drag

				dragCount++;
				dragPosition += contact.point;
				dragVelocity += v;
				dragHardness += hardness;

//				if (showContactGizmos)
//					Debug.DrawLine(contact.point, contact.point + CommonTools.Lin2Log(v), Color.cyan);
			}

			// Debug.DrawLine(contact.point, contact.point + CommonTools.Lin2Log(v), Color.Lerp(Color.cyan, Color.red, Mathf.Abs(dragRatio)));
			if (showContactGizmos)
				Debug.DrawLine(contact.point, contact.point + contact.normal*0.25f, Color.yellow);
		}

		// Accumulate impact values received.

		if (impactCount > 0)
		{
			float invCount = 1.0f / impactCount;
			impactPosition *= invCount;
			impactVelocity *= invCount;

			m_sumImpactCount++;
			m_sumImpactPosition += m_transform.InverseTransformPoint(impactPosition);
			m_sumImpactVelocity += m_transform.InverseTransformDirection(impactVelocity);
			m_sumImpactHardness += impactHardness;
		}

		// Update the current drag value

		if (dragCount > 0)
		{
			float invCount = 1.0f / dragCount;
			dragPosition *= invCount;
			dragVelocity *= invCount;

			UpdateDragState(m_transform.InverseTransformPoint(dragPosition), m_transform.InverseTransformDirection(dragVelocity), dragHardness);
		}
	}

	void UpdateDragState (Vector3 dragPosition, Vector3 dragVelocity, int dragHardness)
	{
		if (dragVelocity.sqrMagnitude > 0.001f)
		{
			m_localDragPosition = Vector3.Lerp(m_localDragPosition, dragPosition, 10.0f * Time.deltaTime);
			m_localDragVelocity = Vector3.Lerp(m_localDragVelocity, dragVelocity, 20.0f * Time.deltaTime);
			m_localDragHardness = dragHardness;
		}
		else
		{
			m_localDragVelocity = Vector3.Lerp(m_localDragVelocity, Vector3.zero, 10.0f * Time.deltaTime);
		}

//		if (showContactGizmos && localDragVelocity.sqrMagnitude > 0.001f)
//			Debug.DrawLine(transform.TransformPoint(localDragPosition), transform.TransformPoint(localDragPosition) + CommonTools.Lin2Log(transform.TransformDirection(localDragVelocity)), Color.cyan, 0.05f, false);
	}

	void HandleImpacts ()
	{
		// Multiple impacts within an impact interval are accumulated and averaged later.

		if (Time.time-m_lastImpactTime >= impactInterval && m_sumImpactCount > 0)
		{
			// Prepare the impact parameters

			float invCount = 1.0f / m_sumImpactCount;

			m_sumImpactPosition *= invCount;
			m_sumImpactVelocity *= invCount;

			// Notify the listeners on the impact

			if (onImpact != null)
			{
				current = this;
				onImpact();
				current = null;
			}

			// debugText = string.Format("Count: {4}  Impact Pos: {0}  Impact Velocity: {1} ({2,5:0.00})  Impact Friction: {3,4:0.00}", localImpactPosition, localImpactVelocity, localImpactVelocity.magnitude, localImpactFriction, m_sumImpactCount);
//			if (showContactGizmos && localImpactVelocity.sqrMagnitude > 0.001f)
//				Debug.DrawLine(transform.TransformPoint(localImpactPosition), transform.TransformPoint(localImpactPosition) + CommonTools.Lin2Log(transform.TransformDirection(localImpactVelocity)), Color.red, 0.2f, false);

			// Reset impact data

			m_sumImpactCount = 0;
			m_sumImpactPosition = Vector3.zero;
			m_sumImpactVelocity = Vector3.zero;
			m_sumImpactHardness = 0;

			m_lastImpactTime = Time.time + impactInterval * UnityEngine.Random.Range(-impactIntervalRandom, impactIntervalRandom);	// Add a random variation for avoiding regularities
		}
	}
	//--abcc

}


