using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

public class DrawWithMouse : MonoBehaviour
{
    Coroutine drawing;
    public InputActionReference drawAction;
    public GameObject linePrefab;

    void Update()
    {
        if (drawAction.action.WasPressedThisFrame())
        {
            StartLine();
        }
        else if (drawAction.action.WasReleasedThisFrame())
        {
            FinishLine();
        }
        
    }

    void StartLine() {
        if (drawing!=null) {
            StopCoroutine(drawing);
        }
        drawing = StartCoroutine(DrawLine());
    }

    void FinishLine() {
        if (drawing!=null) {
            StopCoroutine(drawing);
        }
    }

    IEnumerator DrawLine() {
        GameObject newGameObject = Instantiate(linePrefab, new Vector3(0,0,0), Quaternion.identity);
        LineRenderer line = newGameObject.GetComponent<LineRenderer>();
        line.positionCount = 0;
        while (true)
        {
            Vector3 position = Camera.main.ScreenToWorldPoint (Mouse.current.position.ReadValue());
            position.z = 0;
            line.positionCount++;
            line.SetPosition(line.positionCount-1, position);
            yield return null;
        }
        
    }
}