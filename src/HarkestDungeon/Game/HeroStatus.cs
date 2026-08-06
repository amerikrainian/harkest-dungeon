using System.Collections.Generic;
using Assets.Code.Actor;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// The hero buffer's composition: the vitals of the hero a focused element concerns (a
    /// skill's owner, a story choice's hero, the sheet's paged hero), read live from the actor -
    /// identity (name with class and path), the status-bar HP/stress caption, and speed.
    /// Elements bind it by yielding these lines for <see cref="Core.Buffers.BufferKeys.Hero"/>.
    /// </summary>
    public static class HeroStatus {
        public static IEnumerable<string> Lines(uint actorGuid) => Lines(Actors.Get(actorGuid));

        public static IEnumerable<string> Lines(ActorInstance actor) {
            if (actor == null) {
                yield break;
            }
            string name = Actors.Name(actor);
            string className = GameLoc.TryGet(actor.ActorDataId);
            // A nameless ally (kingdoms militia) reads its class string as its name already.
            if (className == name) {
                className = null;
            }
            string pathName = actor.ActorDataPath == null ? null
                : ActorPathDescription.GetNameString(actor.ActorDataPath,
                    actor.ActorDataClass.m_LocalizationGender, addColor: false);
            yield return SpokenLine.Join(name, className, pathName);
            yield return Actors.StatusLine(actor);
            yield return S.SheetSpeed((int)actor.GetClampedStatValue(ActorStatType.SPEED));
        }
    }
}
