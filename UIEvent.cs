using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{

    // 定义一个委托，用于处理选择数字索引事件。
    public delegate void ChooseNumberIndexHandler(int playerId, int numberIndex);
    // 定义一个委托，用于处理目标选择事件。
    public delegate void ChooseTargetHandler(int playerId);
    // 定义一个委托，用于处理决定是否应用事件。
    public delegate void DecideApplyHandler(bool isApply);

    // 定义一个委托，用于处理选择数字事件。
    public delegate void ChooseNumberHandler(int number);

}
