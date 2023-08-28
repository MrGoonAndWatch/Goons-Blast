using System.Collections.Generic;
using UnityEngine;

public class StunExplosion : Explosion
{
    [SerializeField] private float _stunDuration = 1.0f;
    [SerializeField] private float _knockbackStrength = 5.0f;

    private List<int> _hitPlayers;

    private void Start()
    {
        _hitPlayers = new List<int>();
    }

    public override bool CausesChainExplosions()
    {
        return false;
    }

    public override bool DestroysBlocks()
    {
        return false;
    }

    public override void HitPlayer(PlayerController targetPlayer, Collider collision)
    {
        var targetPhotonId = targetPlayer.GetPhotonViewId();
        if (_hitPlayers.Contains(targetPhotonId)) return;
        
        _hitPlayers.Add(targetPhotonId);
        var hitDir = targetPlayer.transform.position - collision.ClosestPoint(targetPlayer.transform.position);
        targetPlayer.StartRagdoll(_stunDuration, hitDir * _knockbackStrength);
    }
}
