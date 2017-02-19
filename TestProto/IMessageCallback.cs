using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestProto
{
   interface IMessageCallback
   {

      void OnLoginResponse(UserLoginResponse response);
      void OnEnterResponse(ClientCharacterEnterEvent response);

      void OnItemCreate(ClientItemCreateEvent clientItemCreateEvent);
      void OnItemMove(ClientItemMoveEvent clientItemMoveEvent);
      void OnItemDestroy(ClientItemDestroyEvent clientItemDestroyEvent);
      void OnCharacterCreate(ClientCharacterCreateEvent clientCharacterCreateEvent);
   }
}
