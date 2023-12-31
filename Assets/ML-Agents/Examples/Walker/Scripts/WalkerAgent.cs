using System;
using MLAgentsExamples;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;
using Unity.MLAgents.Actuators;

public class WalkerAgent : Agent
{
    public float maximumWalkingSpeed = 999; //The max walk velocity magnitude an agent will be rewarded for
    Vector3 m_WalkDir; //Direction to the target
//    Quaternion m_WalkDirLookRot; //Will hold the rotation to our target


    [Header("Body Parts")] [Space(10)] public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;

    [Header("Orientation")] [Space(10)]
    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    public OrientationCubeController orientationCube;

    JointDriveController m_JdController;

    EnvironmentParameters m_ResetParams;

    public GroundContact handLeft;
    public GroundContact handRight;

    public override void Initialize()
    {
        //Setup each body part
        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(hips);
        m_JdController.SetupBodyPart(chest);
        m_JdController.SetupBodyPart(spine);
        m_JdController.SetupBodyPart(head);
        m_JdController.SetupBodyPart(thighL);
        m_JdController.SetupBodyPart(shinL);
        m_JdController.SetupBodyPart(footL);
        m_JdController.SetupBodyPart(thighR);
        m_JdController.SetupBodyPart(shinR);
        m_JdController.SetupBodyPart(footR);
        m_JdController.SetupBodyPart(armL);
        m_JdController.SetupBodyPart(forearmL);
        m_JdController.SetupBodyPart(handL);
        m_JdController.SetupBodyPart(armR);
        m_JdController.SetupBodyPart(forearmR);
        m_JdController.SetupBodyPart(handR);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
        
        SetResetParameters();
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.position - hips.position));

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Quaternion.FromToRotation(hips.forward, orientationCube.transform.forward));
        sensor.AddObservation(Quaternion.FromToRotation(head.forward, orientationCube.transform.forward));
        
        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }
    
    // Changes from array to ActionBuffers needed
    public override void OnActionReceived(ActionBuffers vectorAction)
    {
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        // Just a variable to shorten the code (vector continuous actions = vas)
        ActionSegment<float> vas = vectorAction.ContinuousActions;

        bpDict[chest].SetJointTargetRotation(vas[++i], vas[++i], vas[++i]);

        bpDict[spine].SetJointTargetRotation(vas[++i], vas[++i], vas[++i]);

        bpDict[thighL].SetJointTargetRotation(vas[++i], vas[++i], 0);
        bpDict[thighR].SetJointTargetRotation(vas[++i], vas[++i], 0);

        bpDict[shinL].SetJointTargetRotation(vas[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(vas[++i], 0, 0);

        bpDict[footR].SetJointTargetRotation(vas[++i], vas[++i], vas[++i]);
        bpDict[footL].SetJointTargetRotation(vas[++i], vas[++i], vas[++i]);

        bpDict[armL].SetJointTargetRotation(vas[++i], vas[++i], 0);
        bpDict[armR].SetJointTargetRotation(vas[++i], vas[++i], 0);

        bpDict[forearmL].SetJointTargetRotation(vas[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(vas[++i], 0, 0);

        bpDict[head].SetJointTargetRotation(vas[++i], vas[++i], 0);

        //update joint strength settings
        bpDict[chest].SetJointStrength(vas[++i]);
        bpDict[spine].SetJointStrength(vas[++i]);
        bpDict[head].SetJointStrength(vas[++i]);
        bpDict[thighL].SetJointStrength(vas[++i]);
        bpDict[shinL].SetJointStrength(vas[++i]);
        bpDict[footL].SetJointStrength(vas[++i]);
        bpDict[thighR].SetJointStrength(vas[++i]);
        bpDict[shinR].SetJointStrength(vas[++i]);
        bpDict[footR].SetJointStrength(vas[++i]);
        bpDict[armL].SetJointStrength(vas[++i]);
        bpDict[forearmL].SetJointStrength(vas[++i]);
        bpDict[armR].SetJointStrength(vas[++i]);
        bpDict[forearmR].SetJointStrength(vas[++i]);
    }

    void FixedUpdate()
    {
        var cubeForward = orientationCube.transform.forward;
        // Set reward for this step according to mixture of the following elements.
        // a. Velocity alignment with goal direction.
        var moveTowardsTargetReward = Vector3.Dot(cubeForward,
            Vector3.ClampMagnitude(m_JdController.bodyPartsDict[hips].rb.velocity, maximumWalkingSpeed));
        if (float.IsNaN(moveTowardsTargetReward))
        {
            throw new ArgumentException(
                "NaN in moveTowardsTargetReward.\n" +
                $" cubeForward: {cubeForward}\n"+
                $" hips.velocity: {m_JdController.bodyPartsDict[hips].rb.velocity}\n"+
                $" maximumWalkingSpeed: {maximumWalkingSpeed}"
            );
        }

        // b. Rotation alignment with goal direction.
        var lookAtTargetReward = Vector3.Dot(cubeForward, head.forward);
        if (float.IsNaN(lookAtTargetReward))
        {
            throw new ArgumentException(
                "NaN in lookAtTargetReward.\n" +
                $" cubeForward: {cubeForward}\n"+
                $" head.forward: {head.forward}"
            );
        }

        // c. Encourage head height. //Should normalize to ~1
        var feetHeigthOverHead =
            ((footL.position.y - head.position.y) + (footR.position.y - head.position.y) / 50);
        if (float.IsNaN(feetHeigthOverHead))
        {
            throw new ArgumentException(
                "NaN in headHeightOverFeetReward.\n" +
                $" head.position: {head.position}\n"+
                $" footL.position: {footL.position}\n"+
                $" footR.position: {footR.position}"
            );
        }

        float groundContactRewardLeft = handLeft.touchingGround ? 1f : -1f;
        float groundContactRewardRight = handRight.touchingGround ? 1f : -1f;

        
        
        AddReward(
            + 0.005f * groundContactRewardLeft
            + 0.005f * groundContactRewardRight
            + 0.005f * feetHeigthOverHead
        );
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
    }

    public void SetTorsoMass()
    {
        m_JdController.bodyPartsDict[chest].rb.mass = m_ResetParams.GetWithDefault("chest_mass", 8);
        m_JdController.bodyPartsDict[spine].rb.mass = m_ResetParams.GetWithDefault("spine_mass", 8);
        m_JdController.bodyPartsDict[hips].rb.mass = m_ResetParams.GetWithDefault("hip_mass", 8);
    }

    public void SetResetParameters()
    {
        SetTorsoMass();
    }
}
