using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TootTally.CustomCosmetics
{
    public class CursorTrail : MonoBehaviour
    {
        private Queue<float> _verticesTimes;
        private List<Vector3> _verticesList;
        private LineRenderer _lineRenderer;
        private float _lifetime;
        private float _trailSpeed;
        private float _refreshRate;
        private static Vector3 _velocity;
        private static Vector3 _positionOffset;

        public void Awake()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _verticesTimes = new Queue<float>();
            _verticesList = new List<Vector3>() { gameObject.transform.position };
            _velocity = Vector3.zero;
            AddPoint(0);
        }

        public void Init(float startWidth, float lifetime, float trailSpeed, float refreshRate, Color startColor, Color endColor, Material defaultMat, Texture2D texture = null)
        {
            _positionOffset = new Vector3(.7f, 0);
            _lineRenderer.sortingOrder = 51;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.textureMode = LineTextureMode.RepeatPerSegment;
            _lineRenderer.startWidth = startWidth;
            _lineRenderer.endWidth = 0;
            _lineRenderer.material = GameObject.Instantiate(defaultMat);
            _lineRenderer.material.renderQueue = 3500;
            if (texture != null)
                _lineRenderer.material.mainTexture = texture;
            _lineRenderer.startColor = startColor;
            _lineRenderer.endColor = endColor;
            _lifetime = lifetime;
            _trailSpeed = trailSpeed;
            _refreshRate = refreshRate;
        }

        public void Update()
        {
            var time = Time.time;
            while (_verticesTimes.Count > 2 && _verticesTimes.Peek() + _lifetime < time)
                RemovePoint();

            if (_verticesTimes.Count > 0 && time - _verticesTimes.Last() > 1f / _refreshRate || _verticesList.Count < 2)
                AddPoint(time);

            _velocity.x = -Time.deltaTime * _trailSpeed;
            for (int i = 1; i < _verticesList.Count; i++)
                _verticesList[i] += _velocity;

            SetFirstVerticePosition();

            _lineRenderer.positionCount = _verticesList.Count;
            _lineRenderer.SetPositions(_verticesList.ToArray());
        }

        private void AddPoint(float time)
        {
            _verticesTimes.Enqueue(time);
            _verticesList.Insert(1, gameObject.transform.position + _positionOffset);
        }

        private void SetFirstVerticePosition()
        {
            _verticesList[0] = gameObject.transform.position + _positionOffset;
        }

        private void RemovePoint()
        {
            _verticesTimes.Dequeue();
            _verticesList.RemoveAt(_verticesList.Count - 1);
        }
    }
}
