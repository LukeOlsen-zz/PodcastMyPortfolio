import React, { Component } from 'react';
import { Button } from 'reactstrap';
import axios from 'axios';
import { authHeader } from '../../_authHeader';


export class ClientSendLoginEmail extends React.Component {
  constructor(props, context) {
    super(props, context);

    this.handleClick = this.handleClick.bind(this);

    this.state = {
      isSending: false,
      sent: false
    };
  }

  componentDidMount() {
    this.props.onRef(this);
  }
  componentWillUnmount() {
    this.props.onRef(undefined);
  }
  reset() {
    this.setState({ sent: false });
  }


  handleClick() {
    this.setState({ isSending: true });
    const fd = new FormData();
    axios.put(`/api/clients/${this.props.clientid}/sendwelcomeemail`, fd, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        this.setState({ isSending: false });
        this.setState({ sent: true });
      })
      .catch(function (error) {
        console.log(error);
        this.setState({ isSending: false });
        this.setState({ sent: false });
      });
  }

  render() {
    const { isSending, sent } = this.state;

    return (
      <Button type="button"
          variant="primary"
          disabled={isSending || sent || this.props.disabled}
          onClick={!isSending ? this.handleClick : null}
      >
      {isSending ? 'Sending...' : sent ? 'Podcast Login Info Emailed' : 'Send Client Podcast Login Email'} 
      </Button>
    );
  }
}

