# @game-guild/quiz-surface

Controlled React authoring, collection, learner, and local-practice surfaces
for `@game-guild/quiz`.

The package owns quiz collection interaction and presentation. Collection
items and the persisted document come from `@game-guild/quiz-content`; trusted
grading policy remains in `@game-guild/grading`. The surface does not own JSON
parsing, storage conversion, HTTP integration, or document versioning.
