using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Converts 2D directional input into a world-space movement vector.
    /// Implement to plug in Cinemachine, custom orbit/isometric/spline cameras, or world-locked axes.
    /// </summary>
    public interface IMovementReferenceFrame
    {
        Vector3 GetForwardOnPlane(Vector3 upAxis);
        Vector3 TransformMoveInput(Vector2 moveInput, Vector3 upAxis);
    }

    /// <summary>
    /// Default reference frame backed by any Transform (such as a Camera or Cinemachine pivot).
    /// </summary>
    public sealed class TransformReferenceFrame : IMovementReferenceFrame
    {
        public Transform ReferenceTransform { get; set; }

        public TransformReferenceFrame(Transform referenceTransform = null)
        {
            ReferenceTransform = referenceTransform;
        }

        public Vector3 GetForwardOnPlane(Vector3 upAxis)
        {
            Transform target = ReferenceTransform;
            if (target == null)
            {
                Camera mainCam = Camera.main;
                target = mainCam != null ? mainCam.transform : null;
            }

            if (target == null)
            {
                return Vector3.forward;
            }

            Vector3 fwd = Vector3.ProjectOnPlane(target.forward, upAxis);
            if (fwd.sqrMagnitude < 1e-5f)
            {
                fwd = Vector3.ProjectOnPlane(target.up, upAxis);
            }

            return fwd.sqrMagnitude > 1e-5f ? fwd.normalized : Vector3.forward;
        }

        public Vector3 TransformMoveInput(Vector2 moveInput, Vector3 upAxis)
        {
            if (moveInput.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            Vector3 clamped = Vector2.ClampMagnitude(moveInput, 1f);
            Vector3 forward = GetForwardOnPlane(upAxis);
            Vector3 right = Vector3.Cross(upAxis, forward).normalized;
            return (right * clamped.x) + (forward * clamped.y);
        }
    }
}
