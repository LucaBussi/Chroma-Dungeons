public interface IAbsorptionReceiver
{
    // Chiamata quando il match cromatico raggiunge la soglia di assorbimento
    void OnAbsorptionThresholdReached(float match01);
}
