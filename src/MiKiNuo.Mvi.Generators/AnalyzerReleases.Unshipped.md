; Unshipped analyzer release

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
MVI2001 | Mvi | Error | Feature declaration must support generation
MVI2002 | Mvi | Error | State must be deeply immutable
MVI2003 | Mvi | Error | Input must be a public instance init property
MVI2004 | Mvi | Error | OnInput must have an exact pure conversion signature
MVI2005 | Mvi | Error | Only one rule may handle an input
MVI2006 | Mvi | Error | Generated input entry must not conflict with declared members
MVI2007 | Mvi | Error | Operation must have an exact instance asynchronous signature
MVI2008 | Mvi | Error | Operation validation must have an exact pure predicate signature
MVI2009 | Mvi | Error | Generated operation entry must not conflict with other members
MVI2010 | Mvi | Error | Operation concurrency must be valid with positive Queue capacity or Parallel concurrency bound
MVI2011 | Mvi | Error | Request handler must have exact instance asynchronous operation and message signature
MVI2012 | Mvi | Error | Request validation must be a unique pure State and Message predicate
MVI2013 | Mvi | Error | Duplicate request and result contract within one Feature
MVI2014 | Mvi | Error | Generated request port entry conflicts with existing or generated members
MVI2015 | Mvi | Error | Request port concurrency bounds and cancellation policy must be valid
MVI2016 | Mvi | Error | Generated factory entry conflicts or preferred constructors are ambiguous
