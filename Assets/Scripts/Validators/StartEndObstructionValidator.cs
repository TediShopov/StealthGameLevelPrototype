using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class StartEndObstructionValidator : MeasureMono
    {
        public override string GetName()
        {
            return "StartEndObstructionValidator";
        }

        protected override string Evaluate()
        {
            var start = Phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
            ContactFilter2D contactFilter2D = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = LayerMask.GetMask("Obstacle")
            };
            if (OverlapWithCollider2d(start, contactFilter2D))
            {
                IsTerminating = true;
                return (-1000).ToString();
            }
            var end = Phenotype.GetComponentInChildren<WinTrigger>().gameObject;
            if (OverlapWithCollider2d(end, contactFilter2D))
            {
                IsTerminating = true;
                return (-1000).ToString();
            }
            return 0.ToString();
        }

        private bool OverlapWithCollider2d(GameObject gameObject, ContactFilter2D filter)
        {
            Collider2D[] res = new Collider2D[5];
            var collider = gameObject.GetComponent<Collider2D>();
            if (collider != null)
            {
                return Physics2D.OverlapCollider(collider, filter, res) > 0;
            }
            else
            {
                return false;
            }
        }

        public override void Init(GameObject phenotype)
        {
            IsValidator = true;
            Phenotype = phenotype;
        }
    }
}