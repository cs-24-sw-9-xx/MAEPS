using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoppingCriteria : MonoBehaviour
{
    public Scrollbar progressBar;
    public TextMeshProUGUI cycleText;
    public TextMeshProUGUI stoppingCriteriaEnbaledText;
    private int _totalCycles;
    private int _currentCycles = 0;

    public StoppingCriteria(int cycles, bool idleStop)
    {
        _totalCycles = cycles;
        stoppingCriteriaEnbaledText.text = idleStop ? "Yes" : "No";
    }

    void Start()
    {
        UpdateProgressBar();
    }

    public void CompleteCycle()
    {
        if (_currentCycles < _totalCycles)
        {
            _currentCycles++;
            UpdateProgressBar();
        }
    }

    private void UpdateProgressBar()
    {
        progressBar.size = (float)_currentCycles / (float)_totalCycles;
        cycleText.text = $"{_currentCycles}/{_totalCycles}";
    }
}
