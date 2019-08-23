﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementObj : MonoBehaviour
{
    private bool hasNetworkAuthority = false;
    private float netSpeed = 0.05f; //MS in seconds (TODO - fix hardcoded value)
    private float lerpDelta = 0f; //used for intepolating server values on client

    private Vector3 originalPos;
    private Vector3 newPos;
    private Quaternion originalTurretRot;
    private Quaternion newTurretRot;
    private float posAngle;
    float x = 0;
    float y = 0;
    float radius = 1f;

    GameObject cylinderPivot;
    float cylinderAngle;
    float cylinderDirection = 1f;
    float cylinderMoveAmount = 30f;
    float cylinderMoveSpeed = 10f;

    ARPeerToPeerSample.Game.GameController gameController;

    // Start is called before the first frame update
    private void Start()
    {
        originalPos = transform.position;
        newPos = originalPos;
        posAngle = 0f;
        cylinderPivot = transform.Find("Pivot").gameObject;
        cylinderAngle = Random.Range(-10f, 10f);

        originalTurretRot = cylinderPivot.transform.rotation;
        newTurretRot = originalTurretRot;

        gameController = GameObject.Find("main").GetComponent<ARPeerToPeerSample.Game.GameController>();
    }

    public void SetNetworkAuthority(bool hasAuth)
    {
        hasNetworkAuthority = hasAuth;
    }

    // Update is called once per frame
    private void Update()
    {
        if(hasNetworkAuthority)
        {
            ServerMove();
        }
        else
        {
            ClientMove();
        }
    }

    private void ServerMove()
    {
        //game object movement on circle
        posAngle += 1 * Time.deltaTime;
        x = Mathf.Cos(posAngle) * radius;
        y = Mathf.Sin(posAngle) * radius;

        newPos = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
        transform.position = newPos;


        //cylinder movement back and forth
        cylinderAngle += (cylinderMoveSpeed * cylinderDirection) * Time.deltaTime;

        if (cylinderAngle > cylinderMoveAmount || cylinderAngle < -cylinderMoveAmount)
        {
            cylinderAngle = cylinderMoveAmount * cylinderDirection;
            cylinderDirection *= -1;
        }

        cylinderPivot.transform.rotation = Quaternion.Euler(new Vector3(90, 0, cylinderAngle));
    }

    private void ClientMove()
    {
        lerpDelta += Mathf.Clamp(Time.deltaTime / netSpeed, 0f, 1f);
        transform.position = Vector3.Lerp(transform.position, newPos, lerpDelta);

        cylinderPivot.transform.rotation = Quaternion.Lerp(cylinderPivot.transform.rotation, newTurretRot, lerpDelta);
    }

    public void NetUpdate(Vector3 pos, Vector3 turretRot)
    {
        newPos = pos;
        newTurretRot = Quaternion.Euler(turretRot);
        lerpDelta = 0.1f;
    }
}
