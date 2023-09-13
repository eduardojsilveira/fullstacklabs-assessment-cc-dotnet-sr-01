using API.Services.Exceptions;
using Lib.Repository.Entities;
using Lib.Repository.Repository;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;


public class BattleController : BaseApiController
{
    private readonly IBattleOfMonstersRepository _repository;

    public BattleController()
    {
    }

    public BattleController(IBattleOfMonstersRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll()
    {
        IEnumerable<Battle> battles = await _repository.Battles.GetAllAsync();
        return Ok(battles);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Add([FromBody] Battle battle)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(battle.MonsterA, nameof(battle.MonsterA));
            ArgumentNullException.ThrowIfNull(battle.MonsterB, nameof(battle.MonsterB));
            var battleWithWinner = await Execute(battle);
            var result = await _repository.Battles.AddAsync(battleWithWinner);
            await _repository.Save();
            return Ok(battleWithWinner);
        }
        catch (ArgumentNullException)
        {
            return BadRequest("Missing ID");
        }
        catch (MonsterNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(int id)
    {
        try
        {
            await _repository.Battles.RemoveAsync(id);
            await _repository.Save();
            return Ok();
        }
        catch (Exception)
        {
            return NotFound();
        }

    }

    public async Task<Battle> Execute(Battle battle)
    {
        var monsterA = await _repository.Monsters.FindAsync(battle.MonsterA);
        var monsterB = await _repository.Monsters.FindAsync(battle.MonsterB);

        if (monsterA is null)
        {
            throw new MonsterNotFoundException($"{battle.MonsterA} is not found");
        }

        if (monsterB is null)
        {
            throw new MonsterNotFoundException($"{battle.MonsterB} is not found");
        }

        var firstAttacker = ReturnFirstMonsterToAttack(monsterA, monsterB);

        var secondAttacker = firstAttacker.Id == monsterA.Id ? monsterB : monsterA;
        var winner = DetermineWinner(firstAttacker, secondAttacker);

        battle.Winner = winner.Id;
        return battle;
    }

    private static Monster DetermineWinner(Monster first, Monster second)
    {
        while (second.Hp > 0 || first.Hp > 0)
        {
            var damageFirst = CalculateDamage(first.Attack, second.Defense);
            second.Hp -= damageFirst;
            var damageSecond = CalculateDamage(second.Attack, first.Defense);
            first.Hp -= damageSecond;

            if (first.Hp == 0 || second.Hp == 0)
            {
                break;
            }

        }

        return first.Hp > 0 ? first : second;
    }

    private static Monster ReturnFirstMonsterToAttack(Monster monsterA, Monster monsterB)
    {
        if (IsMonsterSpeedAreEqual(monsterA, monsterB))
        {
            return ReturnHighestAttack(monsterA, monsterB);
        }

        return ReturnMonsterHighestSpeed(monsterA, monsterB);
    }

    private static bool IsMonsterSpeedAreEqual(Monster monsterA, Monster monsterB)
    {
        return monsterA.Speed == monsterB.Speed;
    }

    private static Monster ReturnHighestAttack(Monster monsterA, Monster monsterB)
    {
        return monsterA.Attack > monsterB.Attack ? monsterA : monsterB;
    }

    private static Monster ReturnMonsterHighestSpeed(Monster monsterA, Monster monsterB)
    {
        return monsterA.Speed > monsterB.Speed ? monsterA : monsterB;
    }

    private static int CalculateDamage(int attack, int defense)
    {
        if (attack <= defense) return 1;
        return attack - defense;
    }
}


