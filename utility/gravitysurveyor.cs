//@ commons rangefinder
public class GravitySurveyor
{
    private readonly string TargetGroupName;

    private Rangefinder.LineSample first;

    public GravitySurveyor(string targetGroupName)
    {
        TargetGroupName = targetGroupName;
    }

    public void HandleCommand(ZACommons commons, string argument,
                              Func<ZACommons, IMyRemoteControl> getRemoteControl)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 3);
        if (parts.Length < 2 || parts[0] != "gsurvey") return;
        var command = parts[1];

        if (command == "origin" || command == "snapshot")
        {
            var reference = getRemoteControl(commons);
            var gravity = reference.GetNaturalGravity();
            if (gravity.LengthSquared() == 0.0) return;

            if (command == "origin")
            {
                first = new Rangefinder.LineSample(reference, gravity);
            }
            else if (command == "snapshot")
            {
                var second = new Rangefinder.LineSample(reference, gravity);

                Vector3D closestFirst, closestSecond;
                if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
                {
                    var center = (closestFirst + closestSecond) / 2.0;
                    var radius = (reference.GetPosition() - center).Length();
                    string label = parts.Length > 2 ? parts[2].Trim().Replace(";", "") : "latest";
                    var targetGroup = commons.GetBlockGroupWithName(TargetGroupName);
                    if (targetGroup != null)
                    {
                        var targetString = string.Format("{0};{1};{2};{3};{4}",
                                                         label,
                                                         center.GetDim(0),
                                                         center.GetDim(1),
                                                         center.GetDim(2),
                                                         radius);

                        ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).ForEach(block => {
                                var textPanel = (IMyTextPanel)block;
                                var lines = GetTextPanelLines(textPanel);
                                lines.AddFirst(targetString);
                                WriteTextPanelLines(textPanel, lines);
                            });
                    }
                }
            }
        }
        else if (command == "up" || command == "down")
        {
            var targetGroup = commons.GetBlockGroupWithName(TargetGroupName);
            if (targetGroup != null)
            {
                ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).ForEach(block => {
                        var textPanel = (IMyTextPanel)block;
                        var lines = GetTextPanelLines(textPanel);
                        if (lines.Count > 1)
                        {
                            if (command == "up")
                            {
                                lines.AddLast(lines.First.Value);
                                lines.RemoveFirst();
                            }
                            else
                            {
                                lines.AddFirst(lines.Last.Value);
                                lines.RemoveLast();
                            }
                            WriteTextPanelLines(textPanel, lines);
                        }
                    });
            }
        }
    }

    private LinkedList<string> GetTextPanelLines(IMyTextPanel textPanel)
    {
        var parts = textPanel.GetPublicText().Split('\n');
        var list = new LinkedList<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            var line = parts[i].Trim();
            if (line.Length > 0) list.AddLast(line);
        }
        return list;
    }

    private void WriteTextPanelLines(IMyTextPanel textPanel, IEnumerable<string> lines)
    {
        textPanel.WritePublicText(string.Join("\n", lines));
    }
}
