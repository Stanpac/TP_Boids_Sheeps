using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardControl : MonoBehaviour
{
    [SerializeField] private float _speed = 1f;
    
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        
        transform.position += (new Vector3(x, 0, y) * (Time.deltaTime * _speed));
    }
}
