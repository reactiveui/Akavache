## Akavache.Http

Akavache.Http is a library that works alongside Akavache to schedule and
prioritize HTTP requests, heavily inspired by Android's Volley and Square's
Picasso.

### Things that Akavache.Http should make really easy and intuitive

* Limiting concurrency via transparently scheduling requests. Save battery by
  not spinning up the radio constantly

* Debouncing concurrent requests to the same URL (like we do today with
  DownloadUrl).

* Grouping related requests...

* ...so that they can be group-cancelled.

* Setting proper priorities and importances (and coming up with a pre-canned
  priority scheme so that developers Don't Have To Think). Priorities affect
  order of operations, importance affects retries (combine these? Would you
  ever have a high-pri request that you wouldn't want to retry?)

* Debuggability (like Volley, be super noisy on the log if requested)

* "Speculative fetches" - mark certain requests as optional, grab the first
  [1-5MB of data based on the Internet
  speed](http://commondatastorage.googleapis.com/io2012/presentations/live%20to%20website/101.pdf),
  then cancel the rest. Use it to fill whatever you can of the cache for data
  you might need later.

* Respecting 301 responses and using them as hints for Akavache's invalidation
  cache

* Rendering images from cache (i.e. Volley's NetworkImageView)

* Implicitly handling app state transition (i.e. if the app gets suspended, we
  cancel all our network requests)

* System for handling periodic updates via back-off, disabling/enabling on
  suspend/resume, coalescing multiple timers, etc, etc, etc.

### How is this better than Volley

* It's cross-platform and uses HttpClient so it works erry'where

* It makes easy things really straightforward via intuitive API design, so if
  you don't know what you're doing and you just do what's obvious, you still
  get a default that's still pretty good.

* Afaik, speculative fetches don't exist in Volley

The idea is, just like Akavache's programming model is, "Pretend nothing is
local and certain requests magically end up faster", Akavache.Http's model
is, "Pretend you have a super fast Internet connection and can queue up as
much data as you want, and we'll give you the data in the most useful order we
can, and some requests will end up going super fast."
