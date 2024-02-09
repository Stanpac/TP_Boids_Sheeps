using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

/// <summary>
/// Implementation of http://www.csc.kth.se/utbildning/kth/kurser/DD143X/dkand13/Group9Petter/report/Martin.Barksten.David.Rydberg.report.pdf
/// </summary>
public class SheepBoid : MonoBehaviour
{
    /// <summary>
    /// Vector created by applying all the herding rules
    /// </summary>
    Vector3 targetVelocity;

    /// <summary>
    /// Actual velocity of the sheep
    /// </summary>
    Vector3 velocity;

    Transform predator;
    
    // Rule Weights
    private float _weightCohesionBase;
    private float _weightCohesionFear;
    private float _weightSeparationBase;
    private float _weightSeparationFear;
    private float _weightAlignmentBase;
    private float _weightAlignmentFear;
    private float _weightEscape;
    private float _weightEnclosed;
    
    // Radius
    [SerializeField] private float _FlightZoneRadus = 7f;
    [SerializeField] private float _alignementZoneRadius  = 3f;
    
    // Debug
    [SerializeField, Header("Debug")] private float _Raylenght = 5f;
    void Start()
    {
        predator = GameObject.FindGameObjectWithTag("Predator").transform;
        _weightCohesionBase = SheepHerd.Instance._weightCohesionBase;
        _weightCohesionFear = SheepHerd.Instance._weightCohesionFear;
        _weightSeparationBase = SheepHerd.Instance._weightSeparationBase;
        _weightSeparationFear = SheepHerd.Instance._weightSeparationFear;
        _weightAlignmentBase = SheepHerd.Instance._weightAlignmentBase;
        _weightAlignmentFear = SheepHerd.Instance._weightAlignmentFear;
        _weightEscape = SheepHerd.Instance._weightEscape;
        _weightEnclosed = SheepHerd.Instance._weightEnclosed;   
    }

    /// <summary>
    /// 3.1
    /// Sigmoid function, used for impact of second multiplier
    /// </summary>
    /// <param name="x">Distance to the predator</param>
    /// <returns>Weight of the rule</returns>
    float P(float x)
    {
        return  1 / Mathf.PI * Mathf.Atan((_FlightZoneRadus - x)/ 0.3f ) + 0.5f;
    }

    /// <summary>
    /// 3.2
    /// Combine the two weights affecting the rules
    /// </summary>
    /// <param name="mult1">first multiplier</param>
    /// <param name="mult2">second multipler</param>
    /// <param name="x">distance to the predator</param>
    /// <returns>Combined weights</returns>
    float CombineWeight(float mult1, float mult2, float x)
    {
        return mult1 + (1 + P(x) * mult2);
    }

    /// <summary>
    /// 3.3
    /// In two of the rules, Separation and Escape, nearby objects are prioritized higher than
    ///those further away. This prioritization is described by an inverse square function
    /// </summary>
    /// <param name="x">Distance to the predator</param>
    /// <param name="s">Softness factor</param>
    /// <returns></returns>
    float Inv(float x, float s)
    {
        float value = x / s + Mathf.Epsilon;
        
        return 1 / (value * value);
    }

    /// <summary>
    /// 3.4
    /// The Cohesion rule is calculated for each sheep s with position sp. The Cohesion vector
    ///coh(s) is directed towards the average position Sp.The rule vector is calculated
    ///with the function
    ///coh(s) = Sp − sp/|Sp − sp|
    /// </summary>
    /// <returns>coh(s) the cohesion vector</returns>
    Vector3 RuleCohesion()
    {
        Vector3 sheepPos = transform.position;
        
        SheepBoid[] sheeps = SheepHerd.Instance.sheeps;
        Vector3 averagePos = Vector3.zero;
        foreach (SheepBoid sheep in sheeps) {
            averagePos += sheep.transform.position;
        }
        
        averagePos /= sheeps.Length;
        Vector3 direction = (averagePos - sheepPos).normalized;
        Debug.DrawRay(sheepPos, direction * _Raylenght , Color.green);
        return direction;
    }

    /// <summary>
    /// 3.5
    /// The Separation rule is calculated for each sheep s with position sp. The contribution
    ///of each nearby sheep si
    ///is determined by the inverse square function of the distance
    ///between the sheep with a softness factor of 1. This function can be seen in Formula
    ///(3.3). The rule vector is directed away from the sheep and calculated with the
    ///function
    ///sep(s) = sum(n,i)(sp − sip/|sp − sip| * inv(|sp − sip|, 1))
    /// </summary>
    /// <returns>sep(s) the separation vector</returns>
    Vector3 RuleSeparation()
    {
        Vector3 sp = transform.position;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < SheepHerd.Instance.sheeps.Length; i++) {
            if (SheepHerd.Instance.sheeps[i] == this) continue;
            Vector3 sip = SheepHerd.Instance.sheeps[i].transform.position;
            sum += (sp - sip).normalized * Inv((sp -sip).magnitude, 1);
        }
        Debug.DrawRay(sp, sum.normalized *_Raylenght, Color.black);
        return sum;
    }

    /// <summary>
    /// 3.6
    /// The Alignment rule is calculated for each sheep s. Each sheep si within a radius of
    ///50 pixels has a velocity siv that contributes equally to the final rule vector.The size
    ///of the rule vector is determined by the velocity of all nearby sheep N.The vector is
    ///directed in the average direction of the nearby sheep.The rule vector is calculated
    ///with the function
    ///ali(s) = sum(Siv,N)
    ///where
    ///N = {si: si ∈ S ∩ |sip − sp| ≤ 50}
    /// </summary>
    /// <returns>ali(s) the alignement vector</returns>
    Vector3 RuleAlignment()
    {
        Vector3 sp = transform.position;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < SheepHerd.Instance.sheeps.Length; i++)  {
            Vector3 sip = SheepHerd.Instance.sheeps[i].transform.position;
            if ((sip - sp).magnitude > _alignementZoneRadius) continue;
            sum += SheepHerd.Instance.sheeps[i].velocity;
        }
        Debug.DrawRay(sp , sum.normalized * _Raylenght, Color.yellow);
        return sum;
    }

    /// <summary>
    /// 3.8
    /// The Escape rule is calculated for each sheep s with a position sp. The size of the
    ///rule vector is determined by inverse square function(3.3) of the distance between
    ///the sheep and predator p with a softness factor of 10. The rule vector is directed
    ///away from the predator and is calculated with the function
    ///esc(s) = sp − pp / |sp − pp| * inv(|sp − pp|, 10)
    /// </summary>
    /// <returns>esc(s) the escape vector</returns>
    Vector3 RuleEscape()
    {
        Vector3 sp = transform.position;
        Vector3 pp = predator.position;
        Vector3 direction = (sp - pp).normalized * Inv((sp - pp).magnitude, 2);
        Debug.DrawRay(sp, direction.normalized * _Raylenght, Color.red);
        return direction;
    }

    /// <summary>
    /// 3.9
    /// Get the intended velocity of the sheep by applying all the herding rules
    /// </summary>
    /// <returns>The resulting vector of all the rules</returns>
    Vector3 ApplyRules()
    {
        Vector3 position = transform.position;
        float x = (position - predator.position).magnitude;
        Vector3 v = CombineWeight(_weightCohesionBase, _weightCohesionFear, (x)) * RuleCohesion() +
                   CombineWeight(_weightSeparationBase, _weightSeparationFear, (x)) * RuleSeparation() +
                   CombineWeight(_weightAlignmentBase, _weightAlignmentFear, (x)) * RuleAlignment() +
                   _weightEnclosed * Pen.Instance.RuleEnclosed(position) +
                   _weightEscape * RuleEscape();
        return v;
    }

    void Update()
    {
        targetVelocity = ApplyRules();
    }

    #region Move

    /// <summary>
    /// Move the sheep based on the result of the rules
    /// </summary>
    void Move()
    {
        float flightZoneRadius = 7;
        //Velocity under which the sheep do not move
        float minVelocity = 0.1f;
        //Max velocity of the sheep
        float maxVelocityBase = 1;
        //Max velocity of the sheep when a predator is close
        float maxVelocityFear = 4;

        float distanceToPredator = (transform.position - predator.position).magnitude;

        //Clamp the velocity to a maximum that depends on the distance to the predator
        float currentMaxVelocity = Mathf.Lerp(maxVelocityBase, maxVelocityFear, 1 - (distanceToPredator / flightZoneRadius));

        targetVelocity = Vector3.ClampMagnitude(targetVelocity, currentMaxVelocity);

        //Ignore the velocity if it's too small
        if (targetVelocity.magnitude < minVelocity)
            targetVelocity = Vector3.zero;

        //Draw the velocity as a blue line coming from the sheep in the scene view
        Debug.DrawRay(transform.position, targetVelocity, Color.blue);

        velocity = targetVelocity;

        //Make sure we don't move the sheep verticaly by misstake
        velocity.y = 0;

        //Move the sheep
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    void LateUpdate()
    {
        Move();
    }
    #endregion
}
