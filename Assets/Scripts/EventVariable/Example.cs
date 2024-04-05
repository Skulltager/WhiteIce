using UnityEngine.UI;
using SheetCodes;

public class Example
{
    private Text healthTextField;

    private int _health;
    private readonly ControlledEventVariable<Example, int> health;

    public Example()
    {
        health = new ControlledEventVariable<Example, int>(this, 100, CheckHealth);
        health.onValueChangeImmediate += OnValueChanged_Health;
    }

    private int CheckHealth(int value)
    {
        if (value < 0)
            return 0;

        return value;
    }

    private void OnValueChanged_Health(int oldValue, int newValue)
    {
        healthTextField.text = newValue.ToString();
    }

    public void TakeDamage()
    {
        health.value -= 10;
    }

    public void HealDamage()
    {
        health.value += 10;
    }
}