﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using System;

using Action = NPBehave.Action;

//using static System.Func<bool>;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAISoccer_gr1 : MonoBehaviour
    {
        
        private CarController m_Car; // the car controller we want to use
        public WheelCollider[] WheelColliders;
        float slideLimit=0.3f;

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;
        WhoBall who_ball;
        GoalCheck goal_check;

        public GameObject[] friends;
        public string friend_tag;
        public GameObject[] enemies;
        public string enemy_tag;

        public GameObject own_goal;
        public GameObject other_goal;
        public Vector3 own_goalNormal;
        public GameObject ball;

        public Root behaviorTree;
        public Blackboard blackboard;
        public bool Shooter; 
        bool starting = true;
        GameObject activeShooter;
        GameObject Goalie;
        int oldScore=0;
        float ballCentreHeight;

        //Def
        GameObject defTarget;
        bool startDef = false;
        string oldBallTag="";

        //Driving
        public int backingCounter=0;
        public float newSpeed;
        public float newSteer;
        public float handBrake;

        Vector3 gizTarget;
        Vector3 gizTarget2;
        Vector3 gizTarget3;
        public bool checkSkid;

        // SlipLimit = 0.3 apparently

        private void Start()
        {
            // get the car controller
            WheelColliders = GetComponentsInChildren<WheelCollider>();

            ball = GameObject.FindGameObjectWithTag("Ball");
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            who_ball = ball.GetComponent<WhoBall>();
            goal_check = ball.GetComponent<GoalCheck>();



            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friend_tag = gameObject.tag;
            if (friend_tag == "Blue"){
                enemy_tag = "Red";
                own_goalNormal = new Vector3(1,0,0);
            }
            else{
                enemy_tag = "Blue";
                own_goalNormal = new Vector3(-1,0,0);
            }
            friends = GameObject.FindGameObjectsWithTag(friend_tag);
            enemies = GameObject.FindGameObjectsWithTag(enemy_tag);



            behaviorTree = new Root(
                new Selector(
                    new BlackboardCondition("OtherTeamAttack", Operator.IS_EQUAL, true, Stops.SELF,
                        new Selector(
                            new Condition(new Func<bool>(()=>CheckDefPos()),
                                new Sequence(
                                    new Action(()=>BearingBall()),
                                    new Action(()=>InterceptBall())
                                )
                            ), 
                            new Sequence(
                                new Action(()=>BearingCar()),
                                new Action(()=>InterceptEnemy())
                            )

                        )
                    ),

                    new Selector(
                        new Condition(new Func<bool>(()=>CheckShooter()),
                            new Sequence(
                                new Action(()=>BearingBallTraj()),
                                new Action(()=>AlignToBall())
                            )
                            
                        ),
                        new Action(()=>SupportRealign())
                    )
                    
                )
            );

            blackboard = behaviorTree.Blackboard;

            
#if UNITY_EDITOR
            Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
            debugger.BehaviorTree = behaviorTree;
#endif

        }

        //Def
        public bool CheckDefPos(){

            float ownDis = Mathf.Abs((transform.position - own_goal.transform.position).x);
            float ballDis = Mathf.Abs((ball.transform.position - own_goal.transform.position).x);

            //if(transform.position.x < ball.transform.position.x){ 

            if(Goalie==gameObject){
                return true;
            }

            if(ownDis < ballDis){
                return true;
            }
            return false;
        }

        void BearingBall(){

        }

        void InterceptBall(){
            Vector3 target = ball.transform.position;
            float speedCap = -1f;
            if(Goalie!=gameObject){
                if(target.y > 4f){
                    (speedCap,target)=calcInAirSpeed();
                }
                Drive(target,speedCap);
            }else{
                float ballDis = Mathf.Abs((ball.transform.position - own_goal.transform.position).x);
                //if(200f>Vector3.Distance(own_goal.transform.position,ball.transform.position)){
                if(60f>ballDis){
                    //Drive(ball.transform.position,-1);
                    if(target.y > 4f){
                        (speedCap,target)=calcInAirSpeed();
                    }
                    
                    Align(target, -1, speedCap);
                }else{
                    SupportRealign();
                }
            }

            
        }
        void BearingCar(){
            if(startDef){
                GameObject targetMin = ball;
                float tempMin=1000;

                List<GameObject> listEnemies=new List<GameObject>();
                foreach (GameObject enemy in enemies) listEnemies.Add(enemy);

                foreach(GameObject friend in friends){
                    listEnemies.Remove(friend.GetComponent<CarAISoccer_gr1>().defTarget);
                }
                
                foreach(GameObject enemy in listEnemies){

                    float enemyDis = Mathf.Abs((enemy.transform.position - other_goal.transform.position).x); // x-distance from enemy goal to enemy
                    float ballDis = Mathf.Abs((ball.transform.position - other_goal.transform.position).x); // x-distance from enemy goal to ball

                    //if(other_goal.transform.position.x > enemy.transform.position.x &&  enemy.transform.position.x > ball.transform.position.x){ // If enemy is between goal and ball?
                    if(enemyDis < ballDis){
                        if(tempMin>Vector3.Distance(enemy.transform.position,ball.transform.position)){
                            tempMin = Vector3.Distance(enemy.transform.position,ball.transform.position);
                            targetMin=enemy;
                        }
                    }
                }
                defTarget=targetMin;
                startDef=false;
            }
        }

        void InterceptEnemy(){ // Implement the equations for missile interception (from the lectures)
            if(defTarget==ball){
                Align(defTarget.transform.position,-1,-1);
            }else{
                Drive(defTarget.transform.position,-1);
            }

        }

        bool checkAirborne(){
            float threshold = 4.0f; // Check if ball above this height
            ballCentreHeight = ball.transform.position.y;
            bool ballInAir = false;
            if(ballCentreHeight > threshold){
                ballInAir = true;
            }
            return ballInAir;
        }


        //Attack
        public bool CheckShooter(){
            float minLength=1000000f;
            float tempLength;
            GameObject minFriend=gameObject;
            foreach(GameObject friend in friends){
                tempLength=Vector3.Distance(ball.transform.position,friend.transform.position);
                if(minLength>tempLength){
                    minFriend=friend;
                    minLength=tempLength;
                }

            }
            activeShooter=minFriend;
            if(minFriend==gameObject){
                return true;
            }
            return false;
        }
        void BearingBallTraj(){}
        void AlignToBall(){
            Vector3 target = ball.transform.position;
            Vector3 GoaltoTarget=(target - other_goal.transform.position).normalized;
            Vector3 linePos=Vector3.Project(transform.position-target,GoaltoTarget)+target;  
            gizTarget3=linePos;
            Debug.DrawRay(target,transform.rotation*new Vector3(0,0,1)*10f,Color.green);
            Debug.DrawRay(target,-GoaltoTarget*10f,Color.green);
            if(30f>Vector3.Angle(transform.rotation*new Vector3(0,0,1),-GoaltoTarget) && 10>Vector3.Distance(transform.position,linePos)){
                Shoot(target);
            }

            else if (false){ // Waka waka

            }else{
                Align(target,-1f,-1f);}

        }
        void Shoot(Vector3 target){
            float speedCap=-1f;
            if(target.y > 4f){
                (speedCap,target)=calcInAirSpeed();
            }
            gizTarget=target;
            Drive(target,-1f);


        }
        
        void SupportRealign(){
            Vector3 target;
            if(gameObject==Goalie){
                target = own_goal.transform.position + own_goalNormal.x*(new Vector3(30,0,0));
                float disToTarget = Vector3.Distance(transform.position,target);
                if(30>Vector3.Angle(transform.rotation*new Vector3(0,0,1),own_goalNormal) && 8>disToTarget){
                    Braking(0f);
                    
                }//else{Align(target,-1f,-1f);}
                else{Align(target,30f,20f);}
                
            }else{
                target = own_goal.transform.position+own_goalNormal.x*(new Vector3(70,0,0));
                //Align(target,-1f,-1f);
                Align(target,30f,20f);

            }
        }

        void Align(Vector3 target,float capSpeedFirst,float capSpeedSecond){
            Vector3 TargetToCar = transform.position-target; 
            Vector3 GoalToTarget = (target - other_goal.transform.position).normalized;  
            float disToTarget = Vector3.Distance(transform.position,target);
            Vector3 dynamicTarget;
            //Debug.DrawRay(target,transform.rotation*new Vector3(0,0,1)*10f,Color.red);
            //Debug.DrawRay(target,own_goalNormal*10f,Color.red);

            if(TargetToCar.x*own_goalNormal.x>0){ // Which side of the ball the car is on
                Vector3 v=new Vector3(-Mathf.Sign(own_goalNormal.x),0,Mathf.Sign(TargetToCar.z)).normalized; 
                dynamicTarget = v*1.5f*disToTarget+target;
                dynamicTarget=CapVtoField(transform.position,dynamicTarget, target);
                Drive(dynamicTarget,capSpeedFirst);
                //Gizmos.DrawCube(dynamicTarget, new Vector3(1, 1, 1));
                
            }else{
                
                dynamicTarget=GoalToTarget*disToTarget*(0.1f*Mathf.Abs(target.z-transform.position.z))*0.45f+target;
                dynamicTarget=CapVtoField(transform.position,dynamicTarget,target);
                
                if(30>Vector3.Angle(transform.rotation*new Vector3(0,0,1),own_goalNormal) && 8>disToTarget){
                    Braking(0f);
                    return;
                
                }
                
                //Gizmos.DrawCube(dynamicTarget, new Vector3(1, 1, 1));

                Drive(dynamicTarget,capSpeedSecond);
                
            }
            
            
            
            gizTarget=dynamicTarget;
            gizTarget2=target;
            

        }

        float Braking(float newSteer){
            Vector3 vel = m_Car.GetComponent<Rigidbody>().velocity;
            handBrake=1;
            if(transform.InverseTransformDirection(vel).z>0.01){
                newSpeed=-1;
            }else if(transform.InverseTransformDirection(vel).z<-0.01){
                newSpeed=1;
            }else{
                newSpeed=0;
            }
            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake); 
            return transform.InverseTransformDirection(vel).z; 
        }

        void Drive(Vector3 target,float speedCap){
            /*if(target.y > 4f){
                (speedCap,target)=calcInAirSpeed();
            }*/

            RaycastHit rayHit;
            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            newSteer = (carToTarget.x / carToTarget.magnitude);
            newSpeed=1;
            handBrake = 0f;
            if(backingCounter<=0 && m_Car.CurrentSpeed>10 && newSteer>0.6){
                handBrake = 1f;
            }

            if(backingCounter>0){
                backingCounter--;
                newSteer = -Mathf.Sign(newSteer);
                if(newSteer == 0f){
                    newSteer = -1f;
                }
                newSpeed = -1;
                m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);
                return;
            }

            checkSkid=skidding();

            if(checkSkid){
                handBrake = 0f;
                newSpeed = 0f;
            }

            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            bool hit = Physics.SphereCast(transform.position,2.0f,steeringPoint,out rayHit,3.5f);
            if(hit && rayHit.collider.transform.root.gameObject.tag == "Untagged"){
                backingCounter = 20;
            }

            if(hit && rayHit.collider.transform.root.gameObject.tag == enemy_tag){
                backingCounter = 7;
            }

            if(hit && rayHit.collider.transform.root.gameObject.tag == friend_tag){
                backingCounter = 7;
            }

            if(0<speedCap && m_Car.CurrentSpeed>speedCap){
                newSpeed=0;
            }

            m_Car.Move(newSteer, newSpeed, newSpeed, handBrake);
            Debug.DrawLine(transform.position,target,Color.yellow);

        }

        Vector3 CapVtoField(Vector3 Start,Vector3 V, Vector3 target){
            // start is the car, V is the position of the dynamic target. target is the "static" target, like the ball, or the position to realign to.
            RaycastHit rayHit;
            target.y = 0;
            bool hit = Physics.SphereCast(Start, 0.001f, V-Start,out rayHit,(V-Start).magnitude); // Raycast from the car to the dynamic target. If there is a wall in the way, we should choose 
            if(hit && rayHit.collider.transform.root.gameObject.tag == "Untagged"){
                
                //V = rayHit.point; // Puts V to be the position of the raycast hit on the wall, from the car to the dynamic target.

                // Reduce to only needing one spherecast, the latter.
                hit = Physics.SphereCast(target, 0.001f, V-target,out rayHit,(V-target).magnitude);

                if(hit && rayHit.collider.transform.root.gameObject.tag == "Untagged"){
                    
                    V = rayHit.point;
                    Vector3 adjustVec = (V-target).normalized;
                    V += -10f*adjustVec;
                }
                

            }
            return V;
        }


        bool skidding(){

            for (int i = 0; i < 4; i++)
            {
                WheelHit wheelHit;
                WheelColliders[i].GetGroundHit(out wheelHit);

                // is the tire slipping above the given threshhold
                if ((Mathf.Abs(wheelHit.sidewaysSlip) >= slideLimit))
                {
                    //print("forward"+(Mathf.Abs(wheelHit.forwardSlip)).ToString());
                    //print("side"+(Mathf.Abs(wheelHit.sidewaysSlip)).ToString());
                    
                    
                    return true;
                }
            }
            return false;
            
        }

        (float,Vector3) calcInAirSpeed(){
            // C#
            List<Vector3> positionList;
            // ...
            
            positionList = new List<Vector3>();
            // ...
            
            // Example maximum of 5 seconds
            int maxIterations = Mathf.RoundToInt(5.0f / Time.fixedDeltaTime);
            Vector3 pos = ball.transform.position;
            Rigidbody ballRigidbody = ball.GetComponent<Rigidbody>();
            Vector3 vel = ballRigidbody.velocity;
            float drag = ballRigidbody.drag;
            positionList.Add(pos);
            float elapsedTime = 0.0f;

            Vector3 landPos = new Vector3 (0, 0, 0);
            
            for(int i = 0; i < maxIterations; i++)
            {
                vel = vel + (Physics.gravity * Time.fixedDeltaTime);
                vel *= drag;
                pos += vel * Time.fixedDeltaTime;
                elapsedTime += Time.fixedDeltaTime;
                positionList.Add(pos);

                if(pos.y < 2.25f){
                    landPos = pos;
                    break;
                }

            }

            float mphSpeed = -1f;


            if(landPos != new Vector3 (0, 0, 0)){
            float disToLanding = Vector3.Distance(transform.position,landPos);
            float minSpeed = disToLanding/elapsedTime;
            mphSpeed = minSpeed/0.44704f;//convert m/s to mph
            }

            else{
                mphSpeed = -1f;
                landPos = ball.transform.position;
            }

            return (mphSpeed, landPos);

        }

        void OnDrawGizmos(){
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(gizTarget, 1);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(gizTarget2, 2);
             Gizmos.color = Color.green;
            Gizmos.DrawSphere(gizTarget3, 1);
        }







        private void FixedUpdate()
        {
            if(starting){
                blackboard["OtherTeamAttack"]=false;
                starting=false;
                behaviorTree.Start();
            
            }
            if(oldScore<goal_check.red_score+goal_check.blue_score){
                oldScore=goal_check.red_score+goal_check.blue_score;
                blackboard["OtherTeamAttack"]=false;
            }

            if(who_ball.WhoTag==enemy_tag && who_ball.WhoTag!=oldBallTag){
                startDef=true;
                oldBallTag=who_ball.WhoTag;
                blackboard["OtherTeamAttack"]=true;
            }else if(who_ball.WhoTag==friend_tag && who_ball.WhoTag!=oldBallTag){
                startDef=false;
                oldBallTag=who_ball.WhoTag;
                blackboard["OtherTeamAttack"]=false;
            }

            float tempMin=1000f;
            GameObject tempGoalie=gameObject;
            foreach(GameObject friend in friends){
                if(friend!=activeShooter){
                    float friendDis = Mathf.Abs((friend.transform.position - own_goal.transform.position).x);
                    if(tempMin>friendDis){
                        tempMin=friendDis;
                        tempGoalie=friend;
                    }
                }
            }
            Goalie=tempGoalie;


            
            



        }
    }
}
