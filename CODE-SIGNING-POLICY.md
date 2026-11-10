# Code signing policy

## CRIS GameRoom

CRIS GameRoom este o aplicație Windows open-source, distribuită sub licența MIT și întreținută în repository-ul public:

https://github.com/bossulica38-sketch/cris-gameroom-native

Solicitarea pentru semnare gratuită prin SignPath Foundation este în pregătire. Mențiunea „Free code signing provided by SignPath.io, certificate by SignPath Foundation” va fi publicată după acceptarea oficială a proiectului.

## Responsabilități

- Autori și committers: administratorii repository-ului care au drept de scriere în proiect.
- Revieweri: maintainerii proiectului; modificările externe sunt verificate înainte de includere.
- Aprobatori: proprietarul repository-ului GitHub `bossulica38-sketch`, responsabil de aprobarea fiecărei versiuni trimise la semnare.

Codul sursă și configurația de compilare sunt păstrate în repository-ul public. Fiecare versiune destinată semnării trebuie să provină dintr-un commit identificabil.

## Integritatea compilării

Aplicația și actualizatorul sunt compilate din sursele proiectului folosind GitHub Actions pe agenți Windows găzduiți de GitHub. Instalatorul este generat din aceste executabile cu NSIS.

Nu folosim certificate autogenerate ca substitut pentru semnarea publică de încredere. Nu păstrăm chei private de semnare în repository sau în webroot.

## Confidențialitate

Aplicația comunică cu serviciul CRIS GameRoom pentru funcțiile cerute de utilizator: autentificare, camere, comunicare în timp real și verificarea/descărcarea actualizărilor. Nu transferă alte informații către sisteme externe fără solicitarea utilizatorului sau a persoanei care instalează ori operează aplicația.

## Lansări și aprobare

Fiecare versiune trimisă la semnare trebuie aprobată manual. Executabilele semnate trebuie să corespundă surselor, configurației de compilare și versiunii publicate.

## Instalare și dezinstalare

Instalatorul Windows oferă dezinstalare automată. Instalarea este per utilizator și nu ar trebui să necesite drepturi de administrator.

## Licență

CRIS GameRoom este distribuit sub licența MIT. Licența și notificările aplicabile sunt incluse în repository.
