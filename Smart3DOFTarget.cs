//
//  Smart3DOFTarget.cs
//
//  Author:
//       Fred Moreau <info@unitycoach.ca>
//
//  Copyright (c) 2019 Frederic Moreau - Unity Coach / Jikkou Publishing Inc.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

// Version 0.1

using UnityEngine;
using UnityEngine.Animations;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Smart 3DOF Target.
/// Sets a position constraint's source weight from a reference transform's forward and up vector match with the target.
/// </summary>
[CanEditMultipleObjects]
public class Smart3DOFTarget : MonoBehaviour
{
	[Tooltip("The transform reference.\nAuto configured with Rig.")]
	[SerializeField] Transform reference;

	[Tooltip("The position constraint.\nAuto configured with Rig.")]
	[SerializeField] PositionConstraint positionConstraint;
	ConstraintSource source;

	[Tooltip ("The difference fwd. angle at which the target weight reaches 1")]
	[SerializeField] float minAngleFwd = 10;
	[Tooltip("The difference fwd. angle at which the target weight reaches 0")]
	[SerializeField] float maxAngleFwd = 30;

	[Tooltip("The difference up angle at which the target weight reaches 1")]
	[SerializeField] float minAngleUp = 30;
	[Tooltip("The difference up angle at which the target weight reaches 0")]
	[SerializeField] float maxAngleUp = 60;

	[Tooltip("Weight ease curve - from 0 to 1")]
	[SerializeField] AnimationCurve curve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 0, 0) });

	[Tooltip("The position constraint's source index.\nAuto configured with Rig.")]
	[SerializeField] int index;

	private float _weight;
	/// <summary>
	/// This Smart3DOF Target's weight.
	/// </summary>
	public float weight
	{
		get { return _weight; }
		private set
		{
			if (Mathf.Abs(value - _weight) > Mathf.Epsilon)
			{
				_weight = value;

				source.weight = curve.Evaluate(_weight);
				positionConstraint.SetSource(index, source);
			}
		}
	}

	private void EvaluateWeight()
	{
		weight = Mathf.InverseLerp(maxAngleFwd, minAngleFwd, Vector3.Angle(transform.forward, reference.forward)) *
			Mathf.InverseLerp(maxAngleUp, minAngleUp, Vector3.Angle(transform.up, reference.up));
	}

	private void Awake()
	{
		if (!reference && Camera.main)
			reference = Camera.main.transform;

		if (!positionConstraint)
			positionConstraint = reference.GetComponent<PositionConstraint>();

		source = positionConstraint.GetSource(index);
	}

	private void LateUpdate()
	{
		EvaluateWeight();
	}

#if UNITY_EDITOR
	[SerializeField] bool showGizmos = true;
	Color GizmoColor = new Color(0, 1, 0, .25f);
	Vector3 GizmoSize = new Vector3(1, 1, 0);

	void OnDrawGizmos()
	{
		if (!showGizmos)
			return;
		Gizmos.color = Color.yellow;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireSphere(Vector3.zero, 0.1f);
		//Gizmos.DrawFrustum(Vector3.zero, minAngleFwd*2, 1f, 0.1f, 1.5f);
		//Gizmos.DrawFrustum(Vector3.zero, maxAngleFwd*2, 1f, 0.05f, 1.5f);
		Gizmos.DrawRay(Vector3.zero, Vector3.forward);
		Gizmos.color = GizmoColor;
		Gizmos.DrawCube(Vector3.forward, GizmoSize);
	}

	private static void BuildView(string name, Vector3 position, Vector3 direction, Transform reference, PositionConstraint constraint, int index, float weight = 0)
	{
		Smart3DOFTarget view = new GameObject(name + " Target").AddComponent<Smart3DOFTarget>();
		view.reference = reference;
		view.positionConstraint = constraint;
		view.transform.position = position;
		//view.transform.position = Vector3.zero;
		view.transform.forward = direction;
		view.index = index;
		ConstraintSource source = new ConstraintSource();
		source.sourceTransform = view.transform;
		source.weight = weight;
		constraint.AddSource(source);
	}

	[MenuItem("UnityCoach/Smart 3DOF/Create Smart3DOF Rig")]
	private static void BuildRig()
	{
		Transform cam = new GameObject("Camera", new System.Type[3] { typeof(Camera), typeof(AudioListener), typeof(PositionConstraint) }).transform;
		cam.position = Vector3.zero;
		PositionConstraint constraint = cam.GetComponent<PositionConstraint>();

		// views
		BuildView("Front", Vector3.zero, Vector3.forward, cam, constraint, 0, 1);
		BuildView("Down", Vector3.down, Vector3.down, cam, constraint, 1, 0);
		BuildView("Up", Vector3.up, Vector3.up, cam, constraint, 2, 0);
		BuildView("Left", Vector3.left, Vector3.left, cam, constraint, 3, 0);
		BuildView("Right", Vector3.right, Vector3.right, cam, constraint, 4, 0);

		constraint.constraintActive = true;
	}

	[MenuItem("UnityCoach/Smart 3DOF/Add Smart3DOF Rig Target")]
	private static void AddRigTarget()
	{
		Smart3DOFTarget b = Selection.activeGameObject.GetComponent<Smart3DOFTarget>();
		Transform cam = b.reference;
		PositionConstraint constraint = cam.GetComponent<PositionConstraint>();

		BuildView("New Target", Vector3.zero, Vector3.forward, cam, constraint, constraint.sourceCount, 0);
	}

	[MenuItem("UnityCoach/Smart 3DOF/Add Smart3DOF Rig Target", validate = true)]
	private static bool AddRigTargetValidate()
	{
		return (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Smart3DOFTarget>() != null);
	}

	[MenuItem("UnityCoach/Smart 3DOF/Duplicate Smart3DOF Rig Target")]
	private static void DuplicateRigTarget()
	{
		Smart3DOFTarget b = Selection.activeGameObject.GetComponent<Smart3DOFTarget>();
		Transform cam = b.reference;
		PositionConstraint constraint = cam.GetComponent<PositionConstraint>();

		BuildView(b.gameObject.name+" Duplicate", b.transform.position, b.transform.forward, cam, constraint, constraint.sourceCount, 0);
	}

	[MenuItem("UnityCoach/Smart 3DOF/Duplicate Smart3DOF Rig Target", validate = true)]
	private static bool DuplicateRigTargetValidate()
	{
		return (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Smart3DOFTarget>() != null);
	}

	[MenuItem("UnityCoach/Smart 3DOF/Remove Smart3DOF Rig Target")]
	private static void RemoveRigTarget()
	{
		Smart3DOFTarget b = Selection.activeGameObject.GetComponent<Smart3DOFTarget>();
		Transform cam = b.reference;
		PositionConstraint constraint = cam.GetComponent<PositionConstraint>();

		constraint.RemoveSource(b.index);
		DestroyImmediate(b.gameObject);
	}

	[MenuItem("UnityCoach/Smart 3DOF/Remove Smart3DOF Rig Target", validate = true)]
	private static bool RemoveRigTargetValidate()
	{
		return (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Smart3DOFTarget>() != null);
	}
#endif
}