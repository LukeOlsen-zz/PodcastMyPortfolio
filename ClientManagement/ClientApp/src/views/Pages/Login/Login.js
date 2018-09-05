import React, { Component } from 'react';
import axios from 'axios';
import { Button, Card, CardBody, CardGroup, Col, Container, Form, Input, InputGroup, InputGroupAddon, InputGroupText, Row, FormFeedback, FormGroup } from 'reactstrap';
import { error } from 'util';

class Login extends Component {
  constructor(props) {
    super(props);
    this.state = {
      username: '',
      password: '',
      loginEntryStatus: false
    };
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  handleUserNameChange = event => {
    this.setState({ loginEntryStatus: false });
    this.setState({ username: event.target.value });
  }

  handlePasswordChange = event => {
    this.setState({ loginEntryStatus: false });
    this.setState({ password: event.target.value });
  }

  handlePasswordKeyPress = (e) => {
    if (e.key === "Enter") {
      this.handleSubmit;
    }
  }

  handleSubmit = event => {
    event.preventDefault();

    axios.post('api/users/authenticate', { username: this.state.username, password: this.state.password }, { headers: { 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          sessionStorage.setItem('token', response.data.token);
          sessionStorage.setItem('userfullname', response.data.fullName);
          sessionStorage.setItem('profileimage', response.data.profileImage);
          window.location.replace("/directory/clients");
        }
      })
      .catch(error => {
        if (error.response.status === 400) {
          sessionStorage.removeItem('token');
          this.setState({ loginEntryStatus: true });
        }
      });
    
  }


  render() {
    return (
      <div className="app flex-row align-items-center">
        <Container>
          <Row className="justify-content-center">
            <Col md="8">
              <CardGroup>
                <Card className="p-4">
                  <CardBody>
                    <Form onSubmit={this.handleSubmit} method="post">
                      <h1>Login</h1>
                      <p className="text-muted">Sign In to your account</p>
                      <FormGroup>
                      <InputGroup className="mb-3">
                        <InputGroupAddon addonType="prepend">
                          <InputGroupText>
                            <i className="icon-user" />
                          </InputGroupText>
                        </InputGroupAddon>
                          <Input type="text" invalid={this.state.loginEntryStatus} placeholder="Username" autoComplete="username" onChange={this.handleUserNameChange} />
                          <FormFeedback>Username may be incorrect</FormFeedback>
                      </InputGroup>
                      <InputGroup className="mb-4">
                      <InputGroupAddon addonType="prepend">
                        <InputGroupText>
                          <i className="icon-lock" />
                        </InputGroupText>
                      </InputGroupAddon>
                          <Input type="password" invalid={this.state.loginEntryStatus} placeholder="Password" autoComplete="current-password" onChange={this.handlePasswordChange} />
                      <FormFeedback>Password may be incorrect</FormFeedback>
                      </InputGroup>
                      </FormGroup>
                      <Row>
                        <Col xs="6">
                          <Button type="submit" color="primary" className="px-4">Login</Button>
                        </Col>
                      </Row>
                    </Form>
                  </CardBody>
                </Card>
                <Card className="text-white bg-primary py-5 d-md-down-none" style={{ width: 44 + '%' }}>
                  <CardBody className="text-center">
                    <div>
                      <h2>Sign up</h2>
                      <p>You can sign up for our service by contacting member services at <a className="text-white" href="mailto:signup@fiatica.com">signup@fiatica.com</a>.</p>
                    </div>
                  </CardBody>
                </Card>
              </CardGroup>
            </Col>
          </Row>
        </Container>
      </div>
    );
  }
}

export default Login;
