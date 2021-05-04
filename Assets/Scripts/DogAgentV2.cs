using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using UnityEngine;

public class DogAgentV2 : Agent
{
    public Transform currentDog;
    public Transform currentPlatform;
    public GameObject polePrefab;
    private GameObject currentPole;
    private bool poleActive = false;

    public Rigidbody rigidDogBody;

    public Transform body;
    public Transform front_right_upper;
    public Transform front_right_lower; 
    public Transform front_left_upper;
    public Transform front_left_lower;
    public Transform back_right_upper;
    public Transform back_right_lower;
    public Transform back_left_upper;
    public Transform back_left_lower;

    public BodyPartCollisions b;
    public BodyPartCollisions fru;
    public BodyPartCollisions frl;
    public BodyPartCollisions flu;
    public BodyPartCollisions fll;
    public BodyPartCollisions bru;
    public BodyPartCollisions brl;
    public BodyPartCollisions blu;
    public BodyPartCollisions bll; 

    private Vector3 targetPole = Vector3.zero;
    private Vector3 directionToTarget = Vector3.zero;
    JointDriveController JDController;

    public void Awake()
    {
        JDController = GetComponent<JointDriveController>();

        JDController.SetupBodyPart(body);
        JDController.SetupBodyPart(front_right_upper);
        JDController.SetupBodyPart(front_right_lower);
        JDController.SetupBodyPart(front_left_upper);
        JDController.SetupBodyPart(front_left_lower);
        JDController.SetupBodyPart(back_right_upper);
        JDController.SetupBodyPart(back_right_lower);
        JDController.SetupBodyPart(back_left_upper);
        JDController.SetupBodyPart(back_left_lower);
    }

    public override void OnEpisodeBegin()
    {
        foreach (var bodyPart in JDController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }
        ResetAllPoleChecks();
        ResetAllGroundChecks();
        currentDog.SetPositionAndRotation(new Vector3(currentPlatform.position.x, currentPlatform.position.y + 1f, currentPlatform.position.z), Quaternion.Euler(0, 0, 0));
        SpawnPole();
        UpdateDirectionToTarget();
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        JDController.bodyPartsDict[front_right_upper].SetJointTargetRotation(vectorAction[0], vectorAction[1], 0);
        JDController.bodyPartsDict[front_right_lower].SetJointTargetRotation(vectorAction[2], 0, 0);
        JDController.bodyPartsDict[front_left_upper].SetJointTargetRotation(vectorAction[3], vectorAction[4], 0);
        JDController.bodyPartsDict[front_left_lower].SetJointTargetRotation(vectorAction[5], 0, 0);
        JDController.bodyPartsDict[back_right_upper].SetJointTargetRotation(vectorAction[6], vectorAction[7], 0);
        JDController.bodyPartsDict[back_right_lower].SetJointTargetRotation(vectorAction[8], 0, 0);
        JDController.bodyPartsDict[back_left_upper].SetJointTargetRotation(vectorAction[9], vectorAction[10], 0);
        JDController.bodyPartsDict[back_left_lower].SetJointTargetRotation(vectorAction[11], 0, 0);

        JDController.bodyPartsDict[front_right_upper].SetJointStrength(vectorAction[12]);
        JDController.bodyPartsDict[front_right_lower].SetJointStrength(vectorAction[13]);
        JDController.bodyPartsDict[front_left_upper].SetJointStrength(vectorAction[14]);
        JDController.bodyPartsDict[front_left_lower].SetJointStrength(vectorAction[15]);
        JDController.bodyPartsDict[back_right_upper].SetJointStrength(vectorAction[16]);
        JDController.bodyPartsDict[back_right_lower].SetJointStrength(vectorAction[17]);
        JDController.bodyPartsDict[back_left_upper].SetJointStrength(vectorAction[18]);
        JDController.bodyPartsDict[back_left_lower].SetJointStrength(vectorAction[19]);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(directionToTarget);
        sensor.AddObservation(body.forward);
        sensor.AddObservation(body.right);
        sensor.AddObservation(body.up);

        sensor.AddObservation(b.getTouchingGround());
        sensor.AddObservation(fru.getTouchingGround());
        sensor.AddObservation(frl.getTouchingGround());
        sensor.AddObservation(flu.getTouchingGround());
        sensor.AddObservation(fll.getTouchingGround());
        sensor.AddObservation(bru.getTouchingGround());
        sensor.AddObservation(brl.getTouchingGround());
        sensor.AddObservation(blu.getTouchingGround());
        sensor.AddObservation(bll.getTouchingGround());

        sensor.AddObservation(rigidDogBody.velocity.normalized);
        sensor.AddObservation(rigidDogBody.angularVelocity.normalized);

        foreach (var bodyPart in JDController.bodyPartsList)
        {
            BodyPartObserve(bodyPart, sensor);
        }

    }

    private void BodyPartObserve(BodyPart bp, VectorSensor sensor)
    {
        if(bp.rb.transform != body)
        {
            sensor.AddObservation(bp.currentXNormalizedRot);
            sensor.AddObservation(bp.currentYNormalizedRot);
            sensor.AddObservation(bp.currentZNormalizedRot);
            sensor.AddObservation(bp.currentStrength / JDController.maxJointForceLimit);
        }
    }

    void FixedUpdate()
    {
        checkGroundContact();
        if (poleTouched())
        {
            rewardForReachingPole();
            RemovePole();
            ResetAllPoleChecks();
        }
        UpdateDirectionToTarget();
        penaltyForTime();
        rewardForMovingTowards();
        rewardForOrientation();
        penaltyForNoTargetVelocity();
    }

    public void UpdateTargetLocation(float x, float y, float z)
    {
        targetPole.x = x;
        targetPole.y = y;
        targetPole.z = z;
    }

    public void UpdateDirectionToTarget()
    {
        //indicates lack of target
        if (targetPole.x == 0f && targetPole.y == 0f && targetPole.z == 0f)
        {
            directionToTarget = Vector3.zero;
        }

        directionToTarget = targetPole - body.position;
        directionToTarget.Normalize();
    }

    public bool poleTouched()
    {
        if (b.getTouchingPole())
        {
            return true;
        }
        if (fru.getTouchingPole())
        {
            return true;
        }
        if (frl.getTouchingPole())
        {
            return true;
        }
        if (flu.getTouchingPole())
        {
            return true;
        }
        if (fll.getTouchingPole())
        {
            return true;
        }
        if (bru.getTouchingPole())
        {
            return true;
        }
        if (brl.getTouchingPole())
        {
            return true;
        }
        if (blu.getTouchingPole())
        {
            return true;
        }
        if (bll.getTouchingPole())
        {
            return true;
        }
        return false;
    }

    public void checkGroundContact()
    {
        if (b.getTouchingGround())
        {
            AddReward(-1);
            EndEpisode();
        }
        else if (fru.getTouchingGround())
        {
            AddReward(-1);
            EndEpisode();
        }
        else if (flu.getTouchingGround())
        {
            AddReward(-1);
            EndEpisode();
        }
        else if (bru.getTouchingGround())
        {
            AddReward(-1);
            EndEpisode();
        }
        else if (blu.getTouchingGround())
        {
            AddReward(-1);
            EndEpisode();
        }
    }

    public void rewardForReachingPole()
    {
        AddReward(1);
    }

    private void penaltyForTime()
    {
        AddReward(-0.001f);
    }

    private void rewardForMovingTowards()
    {
        if (directionToTarget != Vector3.zero)
        {
            float movingTowards = Vector3.Dot(rigidDogBody.velocity, directionToTarget);
            AddReward(0.03f * movingTowards);
        }
    }

    private void rewardForOrientation()
    {
        if (directionToTarget != Vector3.zero)
        {
            Vector3 orientation = body.right;

            float orientedTowards = Vector3.Dot(orientation, directionToTarget);
            AddReward(0.025f * orientedTowards);
        }
    }

    private void penaltyForNoTargetVelocity()
    {
        if(directionToTarget == Vector3.zero)
        {
            AddReward(-0.005f * rigidDogBody.velocity.magnitude);
        }
    }


    public void SpawnPole()
    {
        if (poleActive)
        {
            return;
        }
        //spawn pole at coords
        float platformX = currentPlatform.position.x;
        float platformY = currentPlatform.position.y;
        float platformZ = currentPlatform.position.z;
        float randX = Random.Range(platformX - 25f, platformX + 25f);
        float randZ = Random.Range(platformZ - 25f, platformZ + 25f);
        randX += 0.01f;

        currentPole = Instantiate(polePrefab, new Vector3(randX, platformY + 5f, randZ), Quaternion.Euler(0, 0, 0));
        //give dog coords (randx, platformy, randz)
        UpdateTargetLocation(randX, platformY + 5f, randZ);
        poleActive = true;
    }

    void RemovePole()
    {
        Destroy(currentPole);
        poleActive = false;
        UpdateTargetLocation(0f, 0f, 0f);  // all zeroes indicate the lack of a pole
        StartCoroutine(waitAFewSeconds());
    }

    IEnumerator waitAFewSeconds()
    {
        float randTime = Random.Range(0.01f, 3f);
        yield return new WaitForSeconds(randTime);
        SpawnPole();
    }
    
    public void ResetAllPoleChecks()
    {
        b.ResetPoleChecker();
        fru.ResetPoleChecker();
        frl.ResetPoleChecker();
        flu.ResetPoleChecker();
        fll.ResetPoleChecker();
        bru.ResetPoleChecker();
        brl.ResetPoleChecker();
        blu.ResetPoleChecker();
        bll.ResetPoleChecker();
    }

    public void ResetAllGroundChecks()
    {
        b.ResetGroundChecker();
        fru.ResetGroundChecker();
        frl.ResetGroundChecker();
        flu.ResetGroundChecker();
        fll.ResetGroundChecker();
        bru.ResetGroundChecker();
        brl.ResetGroundChecker();
        blu.ResetGroundChecker();
        bll.ResetGroundChecker();
    }
}
