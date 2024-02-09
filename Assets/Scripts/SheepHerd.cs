using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepHerd : MonoBehaviour {

    [Tooltip("List of all the sheeps in the herd")]
    [HideInInspector]
    public SheepBoid[] sheeps;

    public static SheepHerd Instance;
    
	// Rule Weights
    [Header("RuleWeight")] public float _weightCohesionBase = 0.5f;
    [Header("RuleWeight")] public float _weightCohesionFear = 5;
    [Header("RuleWeight")] public float _weightSeparationBase = 2;
    [Header("RuleWeight")] public float _weightSeparationFear = 0;
    [Header("RuleWeight")] public float _weightAlignmentBase = 0.1f;
    [Header("RuleWeight")] public float _weightAlignmentFear = 1;
    [Header("RuleWeight")] public float _weightEscape = 6;
    [Header("RuleWeight")] public float _weightEnclosed = 3;
    
	void Awake () {
        Instance = this;
	}
	
	void Start () {
        //Find all sheeps in children
        sheeps = GetComponentsInChildren<SheepBoid>(false);
	}
}
