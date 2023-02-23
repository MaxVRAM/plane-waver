using UnityEngine;

using MaxVRAM.Extensions;

public class ApplyForceButtons : MonoBehaviour
{
    public Rigidbody _Rigidbody;
    public float _ForceAmount = 0.1f;
    public float _ForceDecay = 1f;
    public bool _ForceActive = false;
    [Range(-1, 1)] public float _ForceX = 0f;
    [Range(-1, 1)] public float _ForceY = 0f;
    [Range(-1, 1)] public float _ForceZ = 0f;


    void Start()
    {
        if (_Rigidbody == null && TryGetComponent<Rigidbody>(out _Rigidbody))
        {
            Debug.Log($"Could not find target Rigidbody for {name}. Disabling force button script.");
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_ForceActive && Mathf.Approximately(_ForceX, 0) && Mathf.Approximately(_ForceY, 0) && Mathf.Approximately(_ForceZ, 0))
            _ForceActive = false;
    }

    void FixedUpdate()
    {
        if (_Rigidbody != null && gameObject.activeSelf && _ForceActive)
        {
            _Rigidbody.AddForce(new Vector3(_ForceX, _ForceY, _ForceZ) * _ForceAmount, ForceMode.VelocityChange);
            _ForceX = _ForceX.Lerp(0, Time.smoothDeltaTime * _ForceDecay);
            _ForceY = _ForceY.Lerp(0, Time.smoothDeltaTime * _ForceDecay);
            _ForceZ = _ForceZ.Lerp(0, Time.smoothDeltaTime * _ForceDecay);
        }
    }
}
