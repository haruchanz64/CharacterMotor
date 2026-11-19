using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Converts 2D directional input into a world-space movement vector.
    /// Implement to plug in Cinemachine, custom orbit/isometric/spline cameras, or world-locked axes.
    /// </summary>
    public interface IMovementReferenceFrame
    {
        /// <summary>
        /// Returns a unit forward vector projected onto the plane perpendicular to <paramref name="upAxis"/>.
        /// </summary>
        /// <param name="upAxis">World-space up normal defining the locomotion plane.</param>
        /// <returns>Normalized planar forward direction.</returns>
        Vector3 GetForwardOnPlane(Vector3 upAxis);

        /// <summary>
        /// Projects 2D directional input onto the locomotion plane defined by <paramref name="upAxis"/>.
        /// </summary>
        /// <param name="moveInput">Raw 2D stick or keyboard input (X = strafe, Y = forward).</param>
        /// <param name="upAxis">World-space up normal defining the locomotion plane.</param>
        /// <returns>World-space movement direction clamped to unit magnitude, or <see cref="Vector3.zero"/> when input is negligible.</returns>
        Vector3 TransformMoveInput(Vector2 moveInput, Vector3 upAxis);
    }

    /// <summary>
    /// Default reference frame backed by any Transform (such as a Camera or Cinemachine pivot).
    /// </summary>
    public sealed class TransformReferenceFrame : IMovementReferenceFrame
    {
        /// <summary>Explicit reference transform; falls back to <see cref="Camera.main"/> when null.</summary>
        public Transform ReferenceTransform { get; set; }

        /// <summary>
        /// Creates a reference frame targeting <paramref name="referenceTransform"/>, or <see cref="Camera.main"/> if omitted.
        /// </summary>
        /// <param name="referenceTransform">Optional transform whose orientation defines forward movement.</param>
        public TransformReferenceFrame(Transform referenceTransform = null)
        {
            ReferenceTransform = referenceTransform;
        }

        /// <summary>
        /// Projects the target transform's forward axis onto the plane defined by <paramref name="upAxis"/>,
        /// falling back to its up axis when looking straight up or down and to <see cref="Vector3.forward"/> if unassigned.
        /// </summary>
        /// <param name="upAxis">World-space up normal defining the locomotion plane.</param>
        /// <returns>Normalized planar forward direction.</returns>
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
                // Avoid a degenerate zero vector when the camera pitches directly along upAxis.
                fwd = Vector3.ProjectOnPlane(target.up, upAxis);
            }

            return fwd.sqrMagnitude > 1e-5f ? fwd.normalized : Vector3.forward;
        }

        /// <inheritdoc/>
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
