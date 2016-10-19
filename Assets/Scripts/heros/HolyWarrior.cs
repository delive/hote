﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class HolyWarrior : Hero
{

    public override HeroType GetHeroType()
    {
        return HeroType.HolyWarrior;
    }

    public override void Selected()
    {
//        Debug.Log("Holy Warrior hero selected");
    }

    public override void Attack(AttackType type)
    {
        Debug.Log("Holy Warrior attacking with: " + type);
    }

	public override void AutoAttack (GameObject target) {
		Debug.Log ("holy warrior autoattacking" + target);
	}
}