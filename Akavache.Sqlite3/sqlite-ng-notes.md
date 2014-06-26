Here's some completely incomprehensible notes

## High Level Algorithm (aka "The Plan")

* Requests go into a request queue
* Explicit bulk IO just gets splatted out to individual items
* Single background thread is grabbing requests 32 at a time.
* Requests are coalesced, then executed in order
* Queries are pre-prepped and each batch is in a single transaction
* Flush pauses the queue and writes everything out

#### What to coalesce?

* multiple writes to the same key get deduped (unless a read in between!)
* reads to unrelated keys get grouped into bulk query (use writes as boundaries)
* Deletes are like writes, we can use data to service future reads
* Writes to unrelated keys can be bulk inserted
* Unrelated deletes can also be coalesced

#### Coalescing Algorithm

1. GroupBy key, then by original order
2. Simple dedup of multiple people asking for same thing
3. Take all the *first* items from every group
4. Group by request type (insert, etc)
5. Yield the ops out
6. Goto #3 until you're out of items


#### Misc Problems / Notes

* COMPLETION SIGNALLING ORDERING REENTRY ISSUES
* Reentrant requests can't get into the same queue group, does this help?
* Coalescer returns a list of SQL queries to run and how to map result to completion
* Make sure coalescer can be run simply so we can come up with all kinds of evil test cases for it
