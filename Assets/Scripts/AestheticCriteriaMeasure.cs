using UnityEngine;

namespace StealthLevelEvaluation
{
    public abstract class LevelPropertiesEvaluator : MeasureMono
    {
        public float PropertyValue;

        public override string GetName()
        {
            return GetType().Name.ToString();
        }

        public override MeasurementType GetCategory()
        {
            return MeasurementType.PROPERTIES;
        }

        public override void Init(GameObject phenotype)
        {
            Phenotype = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        }

        protected abstract float MeasureProperty();

        protected override string Evaluate()
        {
            try
            {
                PropertyValue = this.MeasureProperty();
                return PropertyValue.ToString();
            }
            catch (System.Exception)
            {
                PropertyValue = 0;
                return "-";
            }
        }
    }
}