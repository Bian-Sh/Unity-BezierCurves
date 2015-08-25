﻿using UnityEngine;
using System.Collections.Generic;

namespace BezierCurves
{
    public class BezierCurve : MonoBehaviour
    {
        // Serializable Fields
        [SerializeField]
        [Tooltip("The color used to render the curve")]
        private Color curveColor = Color.green;

        [SerializeField]
        [Tooltip("The color used to render the start point of the curve")]
        private Color startPointColor = Color.red;

        [SerializeField]
        [Tooltip("The color used to render the end point of the curve")]
        private Color endPointColor = Color.blue;

        [SerializeField]
        [Tooltip("Used only for scene rendering")]
        private int renderSampling = 50;

        [SerializeField]
        [Tooltip("How precise are the calculations. The default value is good for calculations and performance")]
        private int precision = 25;

        [SerializeField]
        [HideInInspector]
        private List<BezierPoint> keyPoints = new List<BezierPoint>();

        // Properties
        public int Precision
        {
            get
            {
                return this.precision;
            }
            set
            {
                this.precision = value;
            }
        }

        public List<BezierPoint> KeyPoints
        {
            get
            {
                return this.keyPoints;
            }
        }

        public int KeyPointsCount
        {
            get
            {
                return this.keyPoints.Count;
            }
        }

        public float ApproximateLength
        {
            get
            {
                float length = 0;
                for (int i = 0; i < this.KeyPointsCount - 1; i++)
                {
                    length += BezierCurve.GetApproximateLengthOfCubicCurve(this.keyPoints[i], this.keyPoints[i + 1], this.Precision);
                }

                return length;
            }
        }

        // Unity Methods
        protected virtual void OnDrawGizmos()
        {
            if (this.KeyPointsCount > 1)
            {
                // Draw the curve
                Gizmos.color = this.curveColor;
                Vector3 fromPoint = this.Evaluate(0f);

                for (int i = 0; i < this.renderSampling; i++)
                {
                    float time = (i + 1) / (float)this.renderSampling;
                    Vector3 toPoint = this.Evaluate(time);
                    Gizmos.DrawLine(fromPoint, toPoint);
                    fromPoint = toPoint;
                }

                // Draw the start and the end of the curve indicators
                Gizmos.color = this.startPointColor;
                Gizmos.DrawSphere(this.keyPoints[0].Position, 0.05f);

                Gizmos.color = this.endPointColor;
                Gizmos.DrawSphere(this.keyPoints[this.KeyPointsCount - 1].Position, 0.05f);
            }
        }

        // Public Methods
        public BezierPoint AddKeyPoint()
        {
            return this.AddKeyPointAt(this.KeyPointsCount);
        }

        public BezierPoint AddKeyPointAt(int index)
        {
            BezierPoint newPoint = new GameObject("Point " + this.keyPoints.Count, typeof(BezierPoint)).GetComponent<BezierPoint>();
            newPoint.Curve = this;
            newPoint.transform.parent = this.transform;
            newPoint.transform.localRotation = Quaternion.identity;

            if (this.KeyPointsCount == 0 || this.KeyPointsCount == 1)
            {
                newPoint.LocalPosition = Vector3.zero;
            }
            else
            {
                if (index == 0)
                {
                    newPoint.Position = (this.keyPoints[0].Position - this.keyPoints[1].Position).normalized + this.keyPoints[0].Position;
                }
                else if (index == this.KeyPointsCount)
                {
                    newPoint.Position = (this.keyPoints[index - 1].Position - this.keyPoints[index - 2].Position).normalized + this.keyPoints[index - 1].Position;
                }
                else
                {
                    newPoint.Position = BezierCurve.EvaluateCubicCurve(0.5f, this.keyPoints[index - 1], this.keyPoints[index]);
                }
            }

            this.keyPoints.Insert(index, newPoint);

            return newPoint;
        }

        public bool RemoveKeyPointAt(int index)
        {
            if (this.KeyPointsCount < 2)
            {
                return false;
            }

            var point = this.keyPoints[index];
            this.keyPoints.RemoveAt(index);

            Destroy(point.gameObject);

            return true;
        }

        public Vector3 Evaluate(float time)
        {
            if (time < 0.01f)
            {
                return this.keyPoints[0].Position;
            }
            else if (Mathf.Abs(time - 1f) < 0.01f)
            {
                return this.keyPoints[this.KeyPointsCount - 1].Position;
            }

            // The evaluated points is between these two points
            BezierPoint startPoint = null;
            BezierPoint endPoint = null;
            float subCurvePercent = 0f;
            float totalPercent = 0f;
            float approximateLength = this.ApproximateLength;
            int subCurvesSampling = (this.Precision / (this.KeyPointsCount - 1)) + 1;

            for (int i = 0; i < this.KeyPointsCount - 1; i++)
            {
                subCurvePercent = BezierCurve.GetApproximateLengthOfCubicCurve(this.keyPoints[i], this.keyPoints[i + 1], subCurvesSampling) / approximateLength;
                if (subCurvePercent + totalPercent > time)
                {
                    startPoint = this.keyPoints[i];
                    endPoint = this.keyPoints[i + 1];

                    break;
                }

                totalPercent += subCurvePercent;
            }


            if (endPoint == null)
            {
                // If the evaluated point is very near to the end of the curve, return the end point
                return this.keyPoints[this.KeyPointsCount - 1].Position;
            }
            else
            {
                float timeRelativeToSubCurve = ((time - totalPercent) / subCurvePercent);
                return BezierCurve.EvaluateCubicCurve(timeRelativeToSubCurve, startPoint, endPoint);
            }
        }

        public static Vector3 EvaluateCubicCurve(float time, BezierPoint startPoint, BezierPoint endPoint)
        {
            return EvaluateCubicCurve(time, startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition);
        }

        public static Vector3 EvaluateCubicCurve(float time, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
        {
            if (time < 0.01f)
            {
                return startPosition;
            }
            else if (Mathf.Abs(time - 1f) < 0.01f)
            {
                return endPosition;
            }

            float u = 1f - time;
            float tt = time * time;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * time;

            Vector3 result = uuu * startPosition; // First term
            result += 3 * uu * time * startTangent; // Second term
            result += 3 * u * tt * endTangent; // Third term
            result += ttt * endPosition; // Fourth term

            return result;
        }

        public static float GetApproximateLengthOfCubicCurve(BezierPoint startPoint, BezierPoint endPoint, int sampling = 10)
        {
            return GetApproximateLengthOfCubicCurve(startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition, sampling);
        }

        public static float GetApproximateLengthOfCubicCurve(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int sampling = 10)
        {
            float length = 0f;
            Vector3 fromPoint = EvaluateCubicCurve(0f, startPosition, endPosition, startTangent, endTangent);

            for (int i = 0; i < sampling; i++)
            {
                float time = (i + 1) / (float)sampling;
                Vector3 toPoint = EvaluateCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
                length += Vector3.Distance(fromPoint, toPoint);
                fromPoint = toPoint;
            }

            return length;
        }
    }
}
