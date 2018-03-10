using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomLights : MonoBehaviour
{
    private void Awake()
    {
        //create random lights

        for (int i = 0; i < 100; ++i)
        {
            GameObject ob = new GameObject("light" + i.ToString());
            Vector3 p = Random.insideUnitSphere * 15;
            p.z *= 0.3f;
            p.z += 10;
            ob.transform.position = p;

            Light l = ob.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = Random.Range(2, 10);
            l.color = Random.ColorHSV();
            l.intensity = Random.Range(0.1f, 8.0f);
        }
    }
}
