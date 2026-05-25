using UnityEngine;
using System.Collections;

public class SheepWobble : MonoBehaviour
{
    Vector3 originalScale;
    Rigidbody rb;
    
    void Start()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Lock rotation so they don't roll down hills
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        
        StartCoroutine(HopRoutine());
    }

    void Update()
    {
        // Re-adding a very lightweight breathing effect
        // 50-100 sheep doing this should be fine for CPU
        float scalePulse = 1.0f + Mathf.Sin(Time.time * 1.5f) * 0.03f;
        transform.localScale = originalScale * scalePulse;
    }

    IEnumerator HopRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 12f));
            
            if (rb != null)
            {
                // Increasing force back up (previous 0.15 was too low)
                rb.AddForce(Vector3.up * Random.Range(1.0f, 1.5f), ForceMode.VelocityChange);
                
                // We'll skip adding torque since we locked rotation to prevent rolling
            }
        }
    }
}
