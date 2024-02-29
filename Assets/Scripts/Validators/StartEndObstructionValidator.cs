using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class StartEndObstructionValidator : PhenotypeFitnessEvaluation
    {
        public override float Evaluate()
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
                return -1000;
            }
            var end = Phenotype.GetComponentInChildren<WinTrigger>().gameObject;
            if (OverlapWithCollider2d(end, contactFilter2D))
            {
                IsTerminating = true;
                return -1000;
            }
            return 0;
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

        // Start is called before the first frame update
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }
}