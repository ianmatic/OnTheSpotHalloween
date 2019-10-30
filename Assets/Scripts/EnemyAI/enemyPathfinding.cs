﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class enemyPathfinding : MonoBehaviour
{

    //House Setup
    public float roomWidth;
    public float roomHeight;
    public float baseY;
    

    [System.Serializable]
    public struct floorDatas
    {
        public roomDatas[] floorData;
        
    }

    [System.Serializable]
    public struct roomDatas
    {
        public Vector2[] roomData;
    }

    public floorDatas[] houseData = new floorDatas[3];

    //TESTING:
    public int playerRealFloor; //TESTING: Player's Actual Floor

    //Public:
    public GameObject enemy;  //Enemy GameObject (Incase not directly attached)
    public GameObject player; //Player GameObject (Until middleman)

    //Private:
    private
    Vector2 enemyPosition; //Enemy's 2D position
    float enemyZPosition;
    //int enemyFloor; //Enemy's current Floor 
    Vector2 playerPosition;//Player's 2D position
    //int playerFloor; //Player's current Floor as known by the Enemy (Won't always be in sync with actual player's floor)
    int playerRoom; //Player's current Room as known by the Enemy (Won't always be in sync with actual player's room)
    Vector2 enemyDestination; //Immidiate Position that Enemy is walking to.
    int enemyTarget; //What type of Destination Enemy is walking to: 0: Player 1: Ladder ?: Eventually a hiding spot or such
    float enemySpeed = 0.04f; //Enemy Speed    Climbing speed is half.
    RoomManager roomManager;
    bool enemyClimbing = false;

    float huntTimer = 0;
    RoomProperties.Type pathTarget = RoomProperties.Type.none;
    RoomProperties.Type pathPreviousTarget = RoomProperties.Type.none;
    List<Vector2> enemyPath = new List<Vector2>();
    RoomProperties wanderDestination;
    /// <summary>
    /// House Consists of several Lists:   From Outer to Inner:
    /// 1. List of Floors
    /// 2. List of Rooms
    /// 3. List of Room Variables
    ///     3a. Vector2D (Ladder: 0=No/1=Yes, Direction: 0=Down/1=Up)
    ///     3b. Vector2D (Center of Room)
    /// </summary>
    public List<List<List<Vector2>>> House = new List<List<List<Vector2>>> ();
    public List<List<RoomProperties>> HouseNew = new List<List<RoomProperties>>();

    private GameObject enemyRoom
    {
        get { return roomManager.EnemyRoomList[0];}
    }
    //Enemy's current Floor 
	private int enemyFloor
	{
        //Get Floor from roomManager's enemyRoom  (0) cause only 1 enemy
		get { return enemyRoom.GetComponent<RoomProperties>().floor;}
	}

    private int playerFloor
    {
        //Get Floor from roomManager's PlayerRoom
		get { return roomManager.PlayerRoom.GetComponent<RoomProperties>().floor;}
    }

    enum State
	{
        Wandering,
        Hunting,
        Climbing,
        Opening,
	}
    State enemyState = State.Wandering;
    // Start is called before the first frame update
    void Start()
    {

        roomManager = GameObject.FindGameObjectWithTag("LevelManager").GetComponent<RoomManager>();
        HouseNew = roomManager.buildHouseNew();
        /*
        for (int i = 0; i < houseData.Length; i++)
		{
            House.Add(new List<List<Vector2>>());
            for (int j = 0; j < houseData[i].floorData.Length; j++)
		    {
                House[i].Add(new List<Vector2>());
                for (int k = 0; k < houseData[i].floorData[j].roomData.Length; k++)
		            {
                        House[i][j].Add(houseData[i].floorData[j].roomData[k]);
		            }
		    }
		}

        //Retrive Room details from (getRooms)  Will pull from Room script in the future.
        //roomManager = GameObject.FindGameObjectWithTag("LevelManager").GetComponent<RoomManager>();
        //House = roomManager.buildHouse();

        //Set Initial enemy Floor, set enemyPosition (Certain Room), and set GameObject position
        enemyFloor = 3;
        
        //TESTING:  sets test player floor,position, and gameobject position
        //playerRealFloor = 2;
        //playerRoom = 1;
        //playerPosition = player.transform.position + new Vector3(0.49f,0, 0);
        */
        

        enemyPosition = enemy.transform.position;
        enemy.transform.position = new Vector3(enemyPosition.x, enemyPosition.y, 0);
        enemyZPosition = 0;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (enemyState)
	{
		case State.Wandering:
                Wander();
            break;
        case State.Hunting:
                Hunt();
            break;
        case State.Climbing:
            moveToDestination();
            break;
        default:
            break;
	}
        /*
        //Grabs the player's Position
        playerPosition = player.transform.position;
        

        //Create new Destination if not on ladder
        
        if (  Mathf.Abs(enemyPosition.y) - ((enemyFloor * roomHeight) + baseY) < 0.01f ){
            enemyClimbing = false;
            createPathToPLayer();
        }
        else {
            enemyClimbing = true;
        }
        //Move to Destination
        moveToDestination();

        */
        if(huntTimer > 0){Debug.Log(huntTimer);}
        if(huntTimer > 0){huntTimer -= Time.deltaTime;}
        Debug.Log(enemyState);
        //Once done all code: Update enemy Gameobject position
        enemy.transform.position = new Vector3(enemyPosition.x, enemyPosition.y, enemyZPosition);
    }

    void moveToDestination()
    {
        if(enemyPath.Count == 0){return;}


        Vector2 direction;
        switch (enemyState)
	    {
	    	case State.Wandering:
                    direction = new Vector2(enemyPath[0].x - enemyPosition.x,0).normalized;
                    enemyPosition.x += direction.x * (enemySpeed/2);
                    switch (pathTarget)
                	{
                		case RoomProperties.Type.none:
                            
                            if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.03f){
                                enemyPath.RemoveAt(0);
                            }
                            break;
                        case RoomProperties.Type.ladder:
                            if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.03f){
                                enemyPath.RemoveAt(0);
                               enemyState = State.Climbing;
                            }
                             break;
                        case RoomProperties.Type.stairs:
                            if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.03f){
                                enemyPath.RemoveAt(0);
                               enemyState = State.Climbing;
                            }
                             break;
                        default:
                             break;
	                }
                break;
            case State.Hunting: 
                direction = new Vector2(enemyPath[0].x - enemyPosition.x,0).normalized;
                enemyPosition.x += direction.x * (enemySpeed);
                switch (pathTarget)
                	{
                		case RoomProperties.Type.none:
                            
                            if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.03f){
                                enemyPath.RemoveAt(0);
                            }
                            break;
                        case RoomProperties.Type.ladder:
                            if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.03f){
                                enemyPath.RemoveAt(0);
                               enemyState = State.Climbing;
                            }
                             break;
                        case RoomProperties.Type.stairs:
                            if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.03f){
                                enemyPath.RemoveAt(0);
                               enemyState = State.Climbing;
                            }
                             break;
                        default:
                             break;
	                }
                break;
            case State.Climbing:
                    switch (pathTarget)
                	{
                        case RoomProperties.Type.ladder:
                            direction = new Vector2(0,enemyPath[0].y - enemyPosition.y).normalized;
                                if(huntTimer > 0){enemyPosition.y += direction.y * (enemySpeed / 1.5f);}
                                else{enemyPosition.y += direction.y * (enemySpeed / 2f);}
                            if(Mathf.Abs( (enemyPosition.y - enemyPath[0].y)) < 0.03f){
                               enemyPath.RemoveAt(0);
                               enemyState = State.Wandering;
                               pathTarget = RoomProperties.Type.none;

                                if(huntTimer > 0){enemyState = State.Hunting;}
                            }
                             break;
                        case RoomProperties.Type.stairs:
                            direction = (enemyPath[0] - enemyPosition).normalized;
                            if(huntTimer > 0){enemyPosition += direction * (enemySpeed / 1.5f);}
                                else{enemyPosition += direction * (enemySpeed / 2f);}
                            if( (enemyPosition - enemyPath[0]).magnitude  < 0.03f){
                               enemyPath.RemoveAt(0);
                               enemyState = State.Wandering;
                               pathTarget = RoomProperties.Type.none;
                               
                                if(huntTimer > 0){enemyState = State.Hunting;}
                            }
                             break;
                        default:
                             break;
	                }

                    //Debug.Log(enemyFloor);

                break;
            default:
                break;
	    }
        
        if(enemyPath.Count == 0){wanderDestination = null;}
        /*
        Debug.Log("Floor: " + enemyFloor);
        Debug.Log("Path Count: " + enemyPath.Count);
        foreach (var item in enemyPath)
	    {
            Debug.Log(item);
	    }
        Debug.Log("Destination: " + enemyPath[0]);
        //Direction of movement (Distance between enemy and its destination, normalized))
        direction = new Vector2(enemyPath[0].x - enemyPosition.x,0).normalized;
        enemyPosition.x += direction.x * enemySpeed;
        if(Mathf.Abs( (enemyPosition.x - enemyPath[0].x)) < 0.3f && Mathf.Abs( (enemyPosition.y - enemyPath[0].y)) <= 1f){
            enemyPath.RemoveAt(0);
            if(enemyPath.Count == 0){wanderDestination = null;}
        }
        */
        //Debug.Log(direction);

        /*
        direction = (enemyDestination - enemyPosition).normalized;

        //Main Switch statement based on what the Destination is: player vs Ladder
        switch (enemyTarget)
        {
            //If Player: Move to Player at base speed untill within set buffer (Buffer code will be replaced by coliders most likely)
            case 0:
                if (Mathf.Abs(enemyPosition.x - enemyDestination.x) > 0.03f)
                {
                    enemyPosition.x += direction.x * enemySpeed;
                    //ONCE within buffer, Set x to exact x of Player  
                    if (Mathf.Abs(enemyPosition.x - enemyDestination.x) <= 0.03f)
                    {
                        enemyPosition.x = enemyDestination.x;
                    }
                }
                break;
            case 1:
                //If Ladder:
                //If X is different then move horizontally to Ladder
                if (Mathf.Abs(enemyPosition.x - enemyDestination.x) > 0.03f)
                {
                    enemyPosition += direction * enemySpeed;
                    if (Mathf.Abs(enemyPosition.x - enemyDestination.x) <= 0.03f)
                    {
                        enemyPosition.x = enemyDestination.x;
                    }
                }
                //If At Ladder, Change Destination to Connected of Next Floor closer to Player
                else
                {
                    //If Y is different, climb.  Else: set Y exactly to Destination and adjust enemyFloor to new Floor
                    if (Mathf.Abs(enemyPosition.y - enemyDestination.y) > 0.03f)
                    {
                        enemyPosition += direction * (enemySpeed * 0.5f);

                    }
                    else
                    {
                        enemyPosition.y = enemyDestination.y;
                        //enemyFloor += (int)direction.y;
                        enemyZPosition = 0;
                    }
                }
                break;
            case 2:
                //If Stairs:
                if ( Mathf.Abs(enemyPosition.x - enemyDestination.x) > 0.03f)
                {
                    if(enemyPosition.y != enemyDestination.y){enemyZPosition = 1;}
                    enemyPosition += direction * enemySpeed;

                    if (Mathf.Abs(enemyPosition.x - enemyDestination.x) <= 0.03f)
                    {
                        enemyPosition.x = enemyDestination.x;
                    }
                }
                else
                {
                    enemyPosition.x = enemyDestination.x;
                    enemyZPosition = 1;
                    /* 
                    if (playerFloor > enemyFloor)
                    {
                        enemyDestination = findLadder(enemyFloor + 1, 0);
                    }
                    else
                    {
                        enemyDestination = findLadder(enemyFloor - 1, 1);
                    }
                    */
                    
        /*
                    //Reset Direction going upwards
                    //direction = (enemyDestination - enemyPosition).normalized;
                    //If Y is different, climb.  Else: set Y exactly to Destination and adjust enemyFloor to new Floor
                    if (Mathf.Abs(enemyPosition.y - enemyDestination.y) > 0.03f)
                    {
                        enemyPosition += direction * (enemySpeed * 0.5f);
                    }
                    else
                    {
                        enemyPosition.y = enemyDestination.y;
                        enemy.transform.position.Set(enemyPosition.x,enemyPosition.y,0);
                       // enemyFloor += (int)direction.y;
                        enemyZPosition = 0;
                    }
                }
                break;
            //No Default: Print Error if Target is ever NOT accounted for by Switch
            default: Debug.Log("ERROR: INVALID EnemyTarget: " + enemyTarget);
                break;
        }
        */
    }

    /// <summary>
    /// Get location of player.
    /// If on same floor: set player as Destination
    /// If on different floor: set closest ladder as Desination
    /// </summary>
    void createPathToPLayer()
    {
        enemyZPosition = 0;
        //Get player floor (Will get information less directly in future)
        //playerFloor = playerRealFloor;
        //Floor Check
        if( Mathf.Abs(enemyPosition.y - playerPosition.y) < 0.2f)
        {
            //Set Destination
            enemyDestination = playerPosition;
            //Set Target as Player
            enemyTarget = 0;
            
        }
        else
        {
            //Determine which Ladder (Up or Down) to go to / Set Destination
            if(playerPosition.y > enemyPosition.y)
            {
                enemyDestination = findLadder(enemyFloor, 1);
                if (Mathf.Abs(enemyPosition.x - enemyDestination.x) < 0.1f )
                    {
                        enemyDestination = findLadder(enemyFloor + 1, 0);
                    }
            }
            else
            {
                enemyDestination = findLadder(enemyFloor, 0);
                if (Mathf.Abs(enemyPosition.x - enemyDestination.x) < 0.1f )
                    {
                        enemyDestination = findLadder(enemyFloor - 1, 1);
                    }
            }
        }
    }
    /// <summary>
    /// Finds Closest Ladder going correct direction
    /// </summary>
    /// <param name="floor">Floor Ladder is located on</param>
    /// <param name="direction">Direction Ladder is going</param>
    /// <returns></returns>
    Vector2 findLadder(int floor, int direction)
    {
        if(floor < 0){floor = 0;}
        //Initial Ladder Room is invalid. POSSIBLE BREAK: If no floor on floor
        int ladderRoom = -1;
        
        //Loop: Each Room on Floor
        /*Debug.Log("floor " + floor);
        Debug.Log("playerFloor " + playerFloor);
        Debug.Log("enemyFloor " + enemyFloor);
        Debug.Log("direction " + direction);*/
        for (int i = 0; i < House[floor].Count; i++)
        {
            //Check: Room is Ladder or Stairs AND going correct direction 
            if( (House[floor][i][0].x >= 1 && (House[floor][i][0].y == direction || House[floor][i][0].y == 2)  ) || House[floor][i][0].x == 3)
            {
                //Check: If NOT first ladderRoom found
                if (ladderRoom != -1)
                {
                    //if( Mathf.Abs(enemyFloor - playerFloor) < 2){
                    //    //Check: If distance to Ladder is less than previous ladderRoom (Based on Player  X then Y)
                    //    if (Mathf.Abs(House[floor][i][1].x - playerPosition.x) < Mathf.Abs(House[floor][ladderRoom][1].x - playerPosition.x))
                    //    {
                    //        ladderRoom = i;
                    //    }
                    //}
                    //else {
                        //Check: If distance to Ladder is less than previous ladderRoom (Based on Enemy Y then X)
                        if (Mathf.Abs(House[floor][i][1].x - enemyPosition.x) < Mathf.Abs(House[floor][ladderRoom][1].x - enemyPosition.x))
                        {
                            ladderRoom = i;
                        }
                    //}
                                      
                }
                //If first ladder found
                else 
                {
                    ladderRoom = i;
                }
            }
        }
        
        //Ladder
        if( House[floor][ladderRoom][0].x == 1 || (House[floor][ladderRoom][0].x >= 3 && House[floor][ladderRoom][0].y == direction)){
            enemyTarget = 1;
        }
        else {
        //Stairs
            enemyTarget = 2;
            return House[floor][ladderRoom][2+direction];
        }
        // Return LadderRoom Position
        return House[floor][ladderRoom][1];
    }

    void findladder(int direction)
    {
        Vector2 firstDestination = new Vector2(0,0);
        Vector2 secondDestination = new Vector2(0,0);
        switch (direction)
	    {
            case 0:
            foreach (RoomProperties room in HouseNew[enemyFloor])
	        {
                if(room.downPath){
                    switch (room.downType)
	                {
                        case RoomProperties.Type.ladder:
                            LadderProperties newLadderProp = room.downProperties.GetComponent<LadderProperties>();
                            Vector2 newLadder = newLadderProp.topLadder.transform.position;
                            if(firstDestination != new Vector2(0,0)){
                                if(Mathf.Abs(enemyPosition.x - firstDestination.x) > Mathf.Abs(enemyPosition.x - newLadder.x)){
                                    firstDestination = newLadder;
                                    secondDestination = newLadderProp.bottomLadder.transform.position;
                                    secondDestination.y+= 0.5f;
                                    pathTarget = RoomProperties.Type.ladder;
                                }
                            } else {
                                firstDestination = newLadder;
                                secondDestination = newLadderProp.bottomLadder.transform.position;
                                secondDestination.y+= 0.5f;
                                pathTarget = RoomProperties.Type.ladder;
                            }
                            break;
                        case RoomProperties.Type.stairs:
                            StairProperties newStairProp = room.downProperties.GetComponent<StairProperties>();
                            Vector2 newStair = newStairProp.topStair.transform.position;
                            if(firstDestination  != new Vector2(0,0)){
                                if(Mathf.Abs(enemyPosition.x - firstDestination.x) > Mathf.Abs(enemyPosition.x - newStair.x)){
                                    firstDestination = newStair;
                                    secondDestination = newStairProp.bottomStair.transform.position;
                                    secondDestination.y+= 0.5f;
                                    pathTarget = RoomProperties.Type.stairs;
                                }
                            } else {
                                firstDestination = newStair;
                                secondDestination = newStairProp.bottomStair.transform.position;
                                secondDestination.y+= 0.5f;
                                pathTarget = RoomProperties.Type.stairs;
                            }
                            break;
                        default:
                            break;
	                }
                }
	        }
                break;
            case 1:
            foreach (RoomProperties room in HouseNew[enemyFloor])
	        {
                if(room.upPath){
                    switch (room.upType)
	                {
                        case RoomProperties.Type.ladder:
                            LadderProperties newLadderProp = room.upProperties.GetComponent<LadderProperties>();
                            Vector2 newLadder = newLadderProp.bottomLadder.transform.position;
                            if(firstDestination  != null){
                                if(Mathf.Abs(enemyPosition.x - firstDestination.x) > Mathf.Abs(enemyPosition.x - newLadder.x)){
                                    firstDestination = newLadder;
                                    secondDestination = newLadderProp.topLadder.transform.position;
                                    secondDestination.y+= 0.5f;
                                    pathTarget = RoomProperties.Type.ladder;
                                }
                            } else {
                                firstDestination = newLadder;
                                secondDestination = newLadderProp.topLadder.transform.position;
                                secondDestination.y+= 0.5f;
                                pathTarget = RoomProperties.Type.ladder;

                            }
                            break;
                        case RoomProperties.Type.stairs:
                            StairProperties newStairProp = room.upProperties.GetComponent<StairProperties>();
                            Vector2 newStair = newStairProp.bottomStair.transform.position;
                            if(firstDestination  != null){
                                if(Mathf.Abs(enemyPosition.x - firstDestination.x) > Mathf.Abs(enemyPosition.x - newStair.x)){
                                    firstDestination = newStair;
                                    secondDestination = newStairProp.topStair.transform.position;
                                    pathTarget = RoomProperties.Type.stairs;
                                }
                            } else {
                                firstDestination = newStair;
                                secondDestination = newStairProp.topStair.transform.position;
                                pathTarget = RoomProperties.Type.stairs;
                            }
                            break;
                        default:
                            break;
	                }
                }
	        }
                break;
		    default:
                break;

            
	    }
        enemyPath.Insert(0, secondDestination);
        enemyPath.Insert(0, firstDestination);
    }

    void Wander(){
        if(wanderDestination){
            moveToDestination();
        } else {
        enemyPath.Clear();
        int chance = Random.Range(0,2);
            //Debug.Log("Chance: " + chance);
        int randomRoom = 0;
        switch (chance)
	        {
                case 0:
                    randomRoom = Random.Range(0,HouseNew[enemyFloor].Count);
                    wanderDestination = HouseNew[enemyFloor][randomRoom];
                    enemyPath.Add(wanderDestination.center);
                    pathTarget = RoomProperties.Type.none;
                    break;
                case 1:
                    chance = Random.Range(0,2);
                    switch (chance)
                	{
                        case 0:
                            if(enemyFloor - 1 >= 0){
                                randomRoom = Random.Range(0,HouseNew[enemyFloor-1].Count);
                                wanderDestination = HouseNew[enemyFloor-1][randomRoom];
                                findladder(0);
                                enemyPath.Add(wanderDestination.center);
                            }
                            break;
                        case 1:
                            if(enemyFloor + 1 < HouseNew.Count){
                                randomRoom = Random.Range(0,HouseNew[enemyFloor+1].Count);
                                wanderDestination = HouseNew[enemyFloor+1][randomRoom];
                                findladder(1);
                                enemyPath.Add(wanderDestination.center);
                            }
                            break;
                		default:
                            break;
                	}
                    
                    break;
	            default:
                    break;
	        }
        }

        if(roomManager.PlayerRoom == enemyRoom /*&& player.hiding == false*/){
            enemyState = State.Hunting;
        }
    }

    void Hunt() {
        if(roomManager.PlayerRoom == enemyRoom /*&& player.hiding == false*/){
            huntTimer = 10f;
            enemyPath.Clear();
            
        }
       RoomProperties playerProp = roomManager.PlayerRoom.GetComponent<RoomProperties>();
        if(/*(roomManager.CurrentStair || roomManager.CurrentLadder) &&*/ player.transform.position.y > enemyPosition.y + 1f){
            findladder(1);
        } else if(/*(roomManager.CurrentStair || roomManager.CurrentLadder) &&*/ player.transform.position.y < enemyPosition.y - 1f){
            findladder(0);
        } else {
            pathTarget = RoomProperties.Type.none;
            
        }
        enemyPath.Add(new Vector2(player.transform.position.x,player.transform.position.y));
        moveToDestination();
        if(huntTimer < 0){ huntTimer = 0; enemyState = State.Wandering; wanderDestination = null;}
    }

    void changeState(State newState){
        switch (newState)
	    {
		    case State.Wandering:
             break;
            case State.Hunting:
             break;
            case State.Climbing:
             break;
            default:
            break;
	    }
    }
}
