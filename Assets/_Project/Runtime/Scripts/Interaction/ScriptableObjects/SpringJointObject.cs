using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "jnt_Spring", menuName = "PlaneWaver/Joint/Spring", order = 1)]
    public class SpringJointObject : BaseJointObject
    {
        public float Spring;
        public float Damper;
        public float MinDistance;
        public float MaxDistance;

        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointEnum => JointType.Spring;
        public override Type GetComponentType => typeof(SpringJoint);

        public override Joint ApplyJointDataToComponent(Joint joint)
        {
            var springJoint = joint as SpringJoint;
            
            if (springJoint == null)
                return null;
         
            base.ApplyJointDataToComponent(springJoint);
            springJoint.spring = Spring;
            springJoint.damper = Damper;
            springJoint.minDistance = MinDistance;
            springJoint.maxDistance = MaxDistance;

            return springJoint;
        }
        
        public override void StoreJointDataFromComponent(Joint joint)
        {
            var springJoint = joint as SpringJoint;
            
            if (springJoint == null)
                return;

            base.StoreJointDataFromComponent(joint);
            Spring = springJoint.spring;
            Damper = springJoint.damper;
            MinDistance = springJoint.minDistance;
            MaxDistance = springJoint.maxDistance;
        }
    }
}
