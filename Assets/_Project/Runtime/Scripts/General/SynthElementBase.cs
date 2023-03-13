using System;
using Unity.Entities;
using UnityEngine;

namespace PlaneWaver
{
    /// <summary>
    /// Abstract class for managing synth elements
    /// </summary>
    public abstract class SynthElement : MonoBehaviour
    {
        #region CLASS DEFINITIONS

        protected int EntityIndex { get; private set; }
        protected EntityManager Manager;
        protected EntityArchetype ElementArchetype;
        protected Entity ElementEntity;
        protected SynthElementType ElementType = SynthElementType.Blank;
        protected bool EntityInitialised;

        #endregion

        #region INITIALISATION METHODS

        public void Awake()
        {
            ElementType = SynthElementType.Blank;
            EntityIndex = int.MaxValue;
            EntityInitialised = false;
        }
        
        protected void InitialiseEntity()
        {
            EntityIndex = SynthManager.Instance.RegisterEntity(this, ElementType);
            
            ElementEntity = Manager.CreateEntity(ElementArchetype);
            name = string.Format("{0}.{1}", ElementType.ToStringCached(), EntityIndex.ToString());
#if UNITY_EDITOR
            Manager.SetName(ElementEntity, name);
#endif
            InitialiseComponents();
            EntityInitialised = true;
        }
        
        protected virtual void InitialiseComponents() { }
        
        #endregion

        #region ENTITY UPDATE LOOP

        public void PrimaryUpdate()
        {
            if (!EntityInitialised || !isActiveAndEnabled)
                return;

            ProcessComponents();
        }

        protected virtual void ProcessComponents() { }

        #endregion

        #region ENTIY MANAGEMENT

        private void OnDestroy()
        {
            DestroyEntity();
        }

        private void DestroyEntity()
        {
            BeforeEntityDestroy();
            try { Manager.DestroyEntity(ElementEntity); }
            catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) { }
        }

        protected virtual void BeforeEntityDestroy() { }

        #endregion
    }
}
