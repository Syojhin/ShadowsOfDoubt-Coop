using System;
using UnityEngine;

namespace ShadowsOfDoubtCoop.Sync
{
    public class RemotePuppet
    {
        private GameObject? _puppetRoot;
        private Transform? _puppetTransform;
        private Light? _flashlight;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isSpawned = false;

        public bool IsSpawned => _isSpawned;

        public void Spawn(Vector3 initialPos, Quaternion initialRot)
        {
            if (_isSpawned && _puppetRoot != null) return;

            try
            {
                // Create Detective Avatar Root
                _puppetRoot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _puppetRoot.name = "RemoteDetective_Puppet";
                _puppetTransform = _puppetRoot.transform;
                _puppetTransform.position = initialPos;
                _puppetTransform.rotation = initialRot;
                _puppetTransform.localScale = new Vector3(0.85f, 1.8f, 0.85f);

                // Disable physical collision with player
                Collider col = _puppetRoot.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                // Noir detective styling (Dark Charcoal Void / Trenchcoat palette)
                Renderer rend = _puppetRoot.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.12f, 0.12f, 0.14f);
                }

                // Add Detective Flashlight
                GameObject lightObj = new GameObject("Detective_Flashlight");
                lightObj.transform.SetParent(_puppetTransform);
                lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0.4f);
                lightObj.transform.localRotation = Quaternion.identity;

                _flashlight = lightObj.AddComponent<Light>();
                _flashlight.type = LightType.Spot;
                _flashlight.range = 18f;
                _flashlight.spotAngle = 45f;
                _flashlight.intensity = 2.5f;
                _flashlight.color = new Color(0.95f, 0.90f, 0.80f); // Warm incandescent flashlight beam
                _flashlight.enabled = true;

                _targetPosition = initialPos;
                _targetRotation = initialRot;
                _isSpawned = true;

                Plugin.Log.LogInfo("[RemotePuppet] Spawned visual detective avatar in world.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[RemotePuppet] Failed to spawn avatar: {ex.Message}");
            }
        }

        public void UpdateTarget(Vector3 pos, Quaternion rot)
        {
            _targetPosition = pos;
            _targetRotation = rot;

            if (!_isSpawned)
            {
                Spawn(pos, rot);
            }
        }

        public void Interpolate(float deltaTime)
        {
            if (!_isSpawned || _puppetTransform == null) return;

            float dist = Vector3.Distance(_puppetTransform.position, _targetPosition);

            // Snap if teleported or respawned > 10m away
            if (dist > 10f)
            {
                _puppetTransform.position = _targetPosition;
                _puppetTransform.rotation = _targetRotation;
            }
            else
            {
                // Smooth dead-reckoning interpolation at 12Hz lerp speed
                _puppetTransform.position = Vector3.Lerp(_puppetTransform.position, _targetPosition, deltaTime * 12f);
                _puppetTransform.rotation = Quaternion.Slerp(_puppetTransform.rotation, _targetRotation, deltaTime * 12f);
            }
        }

        public void SetVisible(bool visible)
        {
            if (_puppetRoot != null)
            {
                _puppetRoot.SetActive(visible);
            }
        }

        public void Destroy()
        {
            if (_puppetRoot != null)
            {
                GameObject.Destroy(_puppetRoot);
                _puppetRoot = null;
                _puppetTransform = null;
                _flashlight = null;
            }
            _isSpawned = false;
        }
    }
}
