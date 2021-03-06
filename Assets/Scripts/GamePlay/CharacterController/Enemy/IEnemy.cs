﻿using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GamePlay.CharacterController.Enemy.Weapon;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.GamePlay.CharacterController.Enemy
{
    [System.Serializable]
    public enum DefaultDirection
    {
        Left,
        Right
    }

    [RequireComponent(typeof(ISkeletonAnimation))]
    public abstract class IEnemy : ICharacter
    {
        public enum EnemyState
        {
            None,
            Idle,
            Patrol,
            Chasing,
            Confusing,
            Rise,
            Fall,
            Attack
        }

        public IWeapon m_weapon = null;
        [Header("Components")]
        public BoxCollider m_boxCollider;
        public Rigidbody m_rigidbody;
        public ISkeletonComponent m_skeletonComponent;

        [Header("Patrol Line Setting")]
        public bool bPatrol = true;
        public DefaultDirection m_defaultDir = DefaultDirection.Right;
        public Transform m_defaultPosition;
        public List<Transform> m_patrolLine;
        public Transform m_currentDestination = null;
        public Transform m_nextDestination = null;

        [Header("Physic Property")]
        public bool bGrounded = true;
        public float m_gravityScale = 6.6f;

        [Header("Behavior Property")]
        public float m_patrolSpeed = 1.0f;
        public float m_chasingSpeed = 2.0f;
        public float m_confusingTime;
        private float m_tempConfusingTime;
        public bool m_isConfusing = false;
        public float m_attackInterval = 1.0f;
        public float m_viewDistance = 20.0f;
        public float m_viewAngle = 180;
        public float m_heightoffset = 3;
        public Transform m_target = null;

        [SerializeField]
        public EnemyState previousState, currentState;
        private GameObject m_visuals = null;

        public bool bTargetInView = false;
        public bool bTargetInAttackRange = false;
        private float moveSpeed = 1.0f;
        public Vector3 mForWardDirection;
        public Vector3 mDefaultDirection;


        protected IEnemy() { }

        public void SetWeapon(IWeapon weapon)
        {
            if (m_weapon != null)
            {
                m_weapon.Release();
            }

            m_weapon = weapon;
            m_weapon.SetOwner(this);
        }

        public IWeapon GetWeapon()
        {
            if (m_weapon == null)
            {
                Debug.LogError("The weapon of " + transform.gameObject + " is missing");
                return null;
            }
            return m_weapon;
        }

        public abstract void Attack(GameObject character);
        public abstract void UnderAttack(GameObject character);


        public override void Init()
        {
            mDefaultDirection = m_defaultDir == DefaultDirection.Right ? Vector3.right : Vector3.left;
            m_tempConfusingTime = m_confusingTime;

            if (bPatrol && m_patrolLine.Count == 0)
            {
                bPatrol = false;
                Debug.LogError("The Patrol Line of " + gameObject + " hasn't been set!");
            }

            if (bPatrol && m_patrolLine.Count >= 2)
            {
                m_currentDestination = m_patrolLine[0];
                m_nextDestination = m_patrolLine[1];
            }

            m_visuals = transform.Find("Visuals").gameObject;
        }

        protected virtual void Update()
        {
            //return to idle state and come back to default position
            //if (!bPatrol)
            //{
            //    Vector3 diretion = (m_defaultPosition.position - transform.position);
            //    m_animator.SetFloat("idleDir", diretion.x);

            //    //check target in patrol states
            //    bTargetInView = ViewCheck(Mathf.Abs(diretion.x) >= 0.2f ? diretion : mForWardDirection);
                
            //    if ( currentState == EnemyState.Idle)
            //    {
            //        FlipXCharacter(Mathf.Abs(diretion.x) >= 0.2f ? diretion.x : mForWardDirection.x);
            //        diretion.x = Mathf.Abs(diretion.x) >= 0.2f ? diretion.normalized.x : 0;
            //        Move(diretion, Gravity());
            //    }
            //}
            //determine line when return to patrol 
            if (bPatrol && previousState == EnemyState.Confusing)
            {
                float minmunDistance = 5.0f;
                foreach (var transform1 in m_patrolLine)
                {
                    if (transform1.position.x - transform.position.x < minmunDistance)
                    {
                        minmunDistance = transform1.position.x - transform.position.x;
                        m_currentDestination = transform1;
                    }
                }
            }
            //Determine next destination in normal line
            if (bPatrol && bTargetInView == false)
            {
                if (Mathf.Abs(m_currentDestination.position.x - transform.position.x) < 0.5f)
                {
                    m_currentDestination = m_nextDestination;
                    for (int i = 0; i < m_patrolLine.Count; i++)
                    {
                        if (m_patrolLine[i] == m_currentDestination)
                        {
                            m_nextDestination = i != m_patrolLine.Count - 1 ? m_patrolLine[i + 1] : m_patrolLine[0];
                        }
                    }
                }
                mForWardDirection = new Vector3(m_currentDestination.position.x - transform.position.x, 0, 0);
            }
            //check target in patrol states
            Vector3 fwd = Mathf.Abs(mForWardDirection.x) >0.2f ? mForWardDirection.normalized : mDefaultDirection;
            bTargetInView = ViewCheck(fwd);

            m_animator.SetFloat("idleDir", mForWardDirection.x);
            m_animator.SetBool("targetinview", bTargetInView);
            //check target is in attack range
            if (bTargetInView)
            {
                bTargetInAttackRange =
                    Mathf.Abs(m_target.position.x - transform.position.x) < m_weapon.m_range;
            }

            //Movement
            if (m_isConfusing)
            {
                if (Mathf.Abs(m_tempConfusingTime) <= 0.1f)
                {
                    m_isConfusing = false;
                    m_tempConfusingTime = m_confusingTime;
                }
                FlipXCharacter(mDefaultDirection.x);
                m_tempConfusingTime -= Time.deltaTime;
            }
            else
            {
                #region patrol routine or move to target
                if (bPatrol && bTargetInView == false)
                {
                    Vector3 diretion = new Vector3(m_currentDestination.position.x - transform.position.x,0,0);
                    if (currentState==EnemyState.Patrol)
                    {
                        FlipXCharacter(Mathf.Abs(diretion.x) >= 0.2f ? diretion.normalized.x : mDefaultDirection.x);
                    }
                    Move(diretion.normalized, Gravity());
                }
                else if (bTargetInView)
                {
                    Vector3 diretion;
                    if (Mathf.Abs(m_target.position.x - transform.position.x) > m_weapon.m_range)
                    {
                        diretion = new Vector3(m_target.position.x - transform.position.x, 0, 0).normalized;
                    }
                    else
                    {
                        diretion = Vector3.zero;
                    }
                    FlipXCharacter(m_target.position.x - transform.position.x);
                    Move(diretion,Vector3.zero);

                }
                #endregion
            }


            //determine next state

            if (bTargetInView)
            {
                currentState = bTargetInAttackRange ? EnemyState.Attack : EnemyState.Chasing;
            }
            else if (bPatrol && bTargetInView == false)
            {
                currentState = (previousState == EnemyState.Chasing || previousState == EnemyState.Attack)
                    ? EnemyState.Confusing : EnemyState.Patrol;
            }
            else if (bPatrol == false)
            {
                if ((previousState == EnemyState.Chasing|| previousState == EnemyState.Attack) 
                    &&bTargetInView==false&&!m_isConfusing)
                {
                    currentState = EnemyState.Confusing;
                }
                else if (!m_isConfusing&& previousState == EnemyState.Confusing)
                {
                    currentState = EnemyState.Idle;
                }

            }

            bool stateChanged = previousState != currentState;//semaphore
            if (stateChanged)
                Debug.Log(previousState + " change to " + currentState);
            previousState = currentState;
            if (stateChanged)
            {
                HandleStateChanged();
            }
        }

        private void Move(Vector3 m_diretionX,Vector3 m_diretionY)
        {
            if (GroundCheck(new Vector3(m_diretionX.x, 0.3f, 0)))
            {
                transform.Translate(m_diretionX * moveSpeed * Time.deltaTime);
            }

            if (!GroundCheck(new Vector3(0, 0.3f, 0)))
            {
                transform.Translate(m_diretionY * moveSpeed * Time.deltaTime);
            }
        }
        private bool ViewCheck(Vector3 forward)
        {
            //Vector3 forward = mDefaultDirection.normalized;//人物前方正方向
            if (m_target == null)
            {
                m_target = GameObject.FindGameObjectWithTag("Player").transform;
            }
            Vector3 playerDir = m_target.transform.position+Vector3.up - (transform.position + Vector3.up * m_heightoffset);//人物到被检测物体的方向
            float tempangle = Vector3.Angle(forward, playerDir);//求出角度
            RaycastHit hitInfo;
            //向被检测物体发射射线，为了判断之间是否有障碍物遮挡
            bool bhit = Physics.Raycast(transform.position + Vector3.up * m_heightoffset, playerDir, out hitInfo, m_viewDistance);
            Debug.DrawRay(transform.position + Vector3.up * m_heightoffset, playerDir.normalized * m_viewDistance, Color.red);
            Debug.DrawRay(transform.position + Vector3.up * m_heightoffset, playerDir.normalized * m_weapon.m_range, Color.yellow);

            if (tempangle < 0.5f * m_viewAngle && (bhit == false || (hitInfo.collider.tag =="Player")))
            {
                if (playerDir.magnitude <= m_viewDistance)//player detected in view distance
                {
                    return true;
                }
            }

            //if (bhit&& tempangle < 10&& hitInfo.transform.tag!="Player"&& playerDir.magnitude <= m_viewDistance)
            //{
            //    Debug.Log("distance" + playerDir.magnitude + "tempangle " + tempangle + "bhit ==" + bhit);
            //    Debug.Log(hitInfo.transform.tag);
            //}
            return false;//被检测物体不在视野中
        }
        private void FlipXCharacter(float x_value)
        {
            var skeleton = m_skeletonComponent.Skeleton;
            if (skeleton != null)
            {
                skeleton.ScaleX = x_value > 0 ? -1 : 1;
            }
        }
        private Vector3 Gravity()
        {
            Vector3 gravityDeltaVelocity = Physics.gravity * m_gravityScale * Time.deltaTime;
            if (!GroundCheck(new Vector3(0, 0.3f, 0)))
            {
                return gravityDeltaVelocity;
            }
            else
            {
                return Vector3.zero;
            }
        }
        private bool GroundCheck(Vector3 Check_offset)
        {
            Debug.DrawRay(transform.position+ Check_offset, Vector2.down * 0.5f, Color.red);
            bool bhit = Physics.Raycast(transform.position + Check_offset, Vector2.down, 0.5f);
            bGrounded = bhit;
            return bGrounded;
        }

        // Determine and store character state

        public void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.tag == "Player" && collider.transform.position.y > transform.position.y + m_heightoffset)
            {
                Debug.Log("hurt!");
                if (currentState == EnemyState.Patrol || currentState == EnemyState.Idle)
                {
                    previousState = currentState;
                    currentState = EnemyState.Confusing;
                }
                bool stateChanged = previousState != currentState;//semaphore
                if (stateChanged)
                    Debug.Log(previousState + " change to " + currentState);
                if (stateChanged)
                {
                    HandleStateChanged();
                }
            }

        }

        public void OnTriggerStay(Collider collider)
        {
            if (collider.gameObject.tag == "Player" && collider.transform.position.y > transform.position.y + m_heightoffset)
            {
                Debug.Log("very hurt!");
                if (currentState == EnemyState.Patrol || currentState == EnemyState.Idle)
                {
                    previousState = currentState;
                    currentState = EnemyState.Chasing;
                }
                else if (currentState == EnemyState.Confusing)
                {
                    previousState = currentState;
                    currentState = EnemyState.Chasing;
                }
                bool stateChanged = previousState != currentState;//semaphore
                if (stateChanged)
                    Debug.Log(previousState + " change to " + currentState);
                if (stateChanged)
                {
                    HandleStateChanged();
                }
            }

        }

        private void HandleStateChanged()
        {
            // When the state changes, notify the animation handle of the new state.
            string stateName = null;
            switch (currentState)
            {
                case EnemyState.Idle:
                    stateName = "idle";
                    //back to default place
                    break;
                case EnemyState.Patrol:
                    stateName = "patrol";
                    moveSpeed = m_patrolSpeed;
                    m_animator.SetTrigger(stateName);
                    break;
                case EnemyState.Chasing:
                    stateName = "chasing";
                    moveSpeed = m_chasingSpeed;
                    m_isConfusing = true;
                    m_animator.SetTrigger("angry");
                    m_animator.SetTrigger(stateName);
                    break;
                case EnemyState.Confusing:
                    stateName = "confusing";
                    m_isConfusing = true;
                    m_animator.SetTrigger(stateName);
                    break;
                case EnemyState.Attack:
                    stateName = "attack";
                    Attack(m_target.gameObject);
                    break;
                //case EnemyState.Crouch:
                //    stateName = "crouch";
                //    m_animator.SetBool(stateName, true);
                //    break;
                //case EnemyState.Rise:
                //    stateName = "rise";
                //    m_animator.SetBool(stateName, true);
                //    m_animator.SetBool("fall", false);
                //    break;
                //case EnemyState.Fall:
                //    stateName = "fall";
                //    m_animator.SetBool(stateName, true);
                //    m_animator.SetBool("rise", false);
                //    break;

                default:
                    break;
            }
        }
    }
}
