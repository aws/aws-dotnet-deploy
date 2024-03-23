const cleanUrl = (url) => {
  const { origin, pathname } = new URL(url);
  return (origin + pathname).replace(new RegExp('/', 'g'), '|');
};

const assetUrl = cleanUrl(window.location.href);

void fetch(`https://prod.us-west-2.tcx-beacon.docs.aws.dev/basic-beacon/${assetUrl}`);
