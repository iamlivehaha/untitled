﻿using UnityEngine;

namespace Assets.Scripts.GamePlay.CharacterController.Enemy.Weapon
{
    public class Pickaxe : IWeapon
    {
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        protected override void ShowAttackEffect()
        {
            throw new System.NotImplementedException();
        }

        public override void Attack(GameObject theTarget)
        {
            throw new System.NotImplementedException();
        }

        public override void Attack(GameObject theTarget, Quaternion rot)
        {
            throw new System.NotImplementedException();
        }
    }
}
