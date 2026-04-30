# NegaMax_CGT390

I implemented NegaMax with alpha-beta pruning to play Othello. NegaMax is just a
cleaner version of minimax for two-player zero-sum games where you negate the
score every time you recurse instead of having separate maximize/minimize branches.
Alpha-beta pruning skips searching branches that can't change the final answer
because the opponent would never let the game reach them.
