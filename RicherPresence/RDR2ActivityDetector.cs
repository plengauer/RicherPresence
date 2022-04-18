using Discord;

public abstract class RDR2ActivityDetector
{

    public abstract void Parse(string text);

    public abstract bool IsActive();
    
    public abstract Activity Create();

}


