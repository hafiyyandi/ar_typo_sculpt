﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sphereHit : MonoBehaviour {

	public float maxRayDistance = 1.0f;
	public LayerMask collisionLayer = 1 << 10;  //ARKitPlane layer

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown (0)) 
		{
			Debug.Log ("CASTING RAY!");
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;

			//we'll try to hit one of the plane collider gameobjects that were generated by the plugin
			//effectively similar to calling HitTest with ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent
			if (Physics.Raycast (ray, out hit, maxRayDistance)) 
			{
				Debug.Log ("HITTING AN OBJECT: "+ hit.collider.gameObject.name);

				if (hit.collider.gameObject.CompareTag ("sphere")) {
					Debug.Log ("HITTING A SPHERE!");
					hit.collider.SendMessageUpwards ("GetHit");
				}


				//CreateBall (new Vector3 (hit.point.x, hit.point.y + createHeight, hit.point.z));

				//we're going to get the position from the contact point
				//Debug.Log (string.Format ("x:{0:0.######} y:{1:0.######} z:{2:0.######}", hit.point.x, hit.point.y, hit.point.z));
			}
		}
		
	}
}
