using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceObject : MonoBehaviour
{
    public GameObject gameObjectToInstantiate;
    public ARPlaneManager planeManager;
    public Button togglePlaneButton;
    public Button rotateButton; // New button reference
    public Scrollbar sizeScrollbar; // UI Scrollbar reference for scaling
    public float minScale = 0.1f, maxScale = 2.0f; // Scale range
    public float rotationDuration = 2.0f; // Duration of the rotation (in seconds)

    private GameObject spawnedObject;
    private ARRaycastManager raycastManager;
    private bool isPlaneDetectionActive = true; // Default: Plane detection is on
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        if (togglePlaneButton != null)
        {
            togglePlaneButton.onClick.AddListener(TogglePlaneDetection);
        }

        if (sizeScrollbar != null)
        {
            sizeScrollbar.onValueChanged.AddListener(UpdateObjectScale);
        }

        if (rotateButton != null)
        {
            rotateButton.onClick.AddListener(RotateObjectGradually); // Set the listener for the new button
        }
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
        touchPosition = default;
        return false;
    }

    void Update()
    {
        if (!isPlaneDetectionActive || !TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                spawnedObject = Instantiate(gameObjectToInstantiate, hitPose.position, hitPose.rotation);
                UpdateObjectScale(sizeScrollbar.value); // Set initial scale
            }
            else
            {
                spawnedObject.transform.position = hitPose.position;
            }
        }
    }

    public void TogglePlaneDetection()
    {
        isPlaneDetectionActive = !isPlaneDetectionActive;
        if (planeManager != null)
        {
            planeManager.enabled = isPlaneDetectionActive;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(isPlaneDetectionActive);
            }
        }
        Debug.Log("Plane Detection: " + (isPlaneDetectionActive ? "Enabled" : "Disabled"));
    }

    public void UpdateObjectScale(float value)
    {
        if (spawnedObject != null)
        {
            float scaleFactor = Mathf.Lerp(minScale, maxScale, value);
            spawnedObject.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    public void RotateObjectGradually()
    {
        if (spawnedObject != null)
        {
            StartCoroutine(RotateObjectCoroutine(spawnedObject.transform, 180f, rotationDuration));
        }
    }

    private IEnumerator RotateObjectCoroutine(Transform objTransform, float targetAngle, float duration)
    {
        Quaternion startRotation = objTransform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, targetAngle, 0);
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            objTransform.rotation = Quaternion.Slerp(startRotation, endRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        objTransform.rotation = endRotation; // Ensure it ends exactly at the target angle
    }
}
